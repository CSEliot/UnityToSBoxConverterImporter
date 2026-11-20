using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sandbox;
using SkiaSharp;

namespace ImportUnityPackage;

/// <summary>CPU image work only. Every worker owns its decoder, bitmap and pixel arrays.</summary>
public static class TextureChannels
{
	public const long MemoryBudget = 1024L * 1024 * 1024;
	public const int WorkerLimit = 2;

	public static void ExtractBatch( IReadOnlyList<TextureChannelRequest> requests, CancellationToken cancel )
		=> ProcessBatch( requests, cancel );

	public sealed record BatchStats( int Decodes, int Channels, int PeakWorkers, long PeakReservedBytes, long ElapsedMilliseconds );

	public static BatchStats ProcessBatch( IReadOnlyList<TextureChannelRequest> requests, CancellationToken cancel, int workers = WorkerLimit, long budget = MemoryBudget )
	{
		if ( workers < 1 || workers > WorkerLimit || budget <= 0 ) throw new ArgumentOutOfRangeException();
		var timer = System.Diagnostics.Stopwatch.StartNew();
		var gate = new object();
		long reserved = 0, peakReserved = 0;
		int active = 0, peakWorkers = 0, decodes = 0;
		var groups = requests.GroupBy( r => r.Source, StringComparer.OrdinalIgnoreCase ).Select( g => g.ToArray() ).ToArray();
		Parallel.ForEach( groups, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = cancel }, group =>
		{
			// Unknown codecs and images larger than the admission budget run exclusively.
			// This is an estimated working-set budget, not a hard cap on native allocations.
			var reservation = Math.Min( budget, EstimateMemory( group[0].Source, budget ) );
			lock ( gate )
			{
				while ( reserved + reservation > budget ) { cancel.ThrowIfCancellationRequested(); Monitor.Wait( gate, 50 ); }
				cancel.ThrowIfCancellationRequested();
				reserved += reservation;
				peakReserved = Math.Max( reserved, peakReserved );
				peakWorkers = Math.Max( ++active, peakWorkers );
			}
			try
			{
				using var bitmap = Decode( group[0].Source, cancel );
				Interlocked.Increment( ref decodes );
				var sourcePixels = bitmap.GetPixels();
				var outputPixels = new Color[sourcePixels.Length];
				foreach ( var request in group )
				{
					cancel.ThrowIfCancellationRequested();
					ConvertPixels( sourcePixels, outputPixels, request.Channel, request.Scale, request.Invert, cancel );
					bitmap.SetPixels( outputPixels );
					File.WriteAllBytes( request.Destination, bitmap.ToPng() );
				}
			}
			catch ( OperationCanceledException ) { throw; }
			catch ( Exception ex ) { throw new InvalidDataException( $"Texture channel extraction failed for '{group[0].Source}': {ex.Message}", ex ); }
			finally { lock ( gate ) { reserved -= reservation; active--; Monitor.PulseAll( gate ); } }
		} );
		return new( decodes, requests.Count, peakWorkers, peakReserved, timer.ElapsedMilliseconds );
	}

	static long EstimateMemory( string source, long unknown )
	{
		using var stream = File.OpenRead( source );
		using var codec = SKCodec.Create( stream );
		if ( codec == null ) return unknown;
		// Decoded image, two float-color arrays, encoder buffers and compressed input.
		return checked( (long)codec.Info.Width * codec.Info.Height * 64 + stream.Length * 2 + 16 * 1024 * 1024 );
	}

	static Bitmap Decode( string source, CancellationToken cancel )
	{
		cancel.ThrowIfCancellationRequested();
		return Bitmap.CreateFromBytes( File.ReadAllBytes( source ) ) ?? throw new InvalidDataException( "Unable to decode packed texture: " + source );
	}

	// Retained for callers supplying a single-channel callback.
	public static byte[] Extract( string source, int channel, double scale, bool invert, CancellationToken cancel )
	{
		using var bitmap = Decode( source, cancel );
		var pixels = bitmap.GetPixels();
		ConvertPixels( pixels, pixels, channel, scale, invert, cancel );
		bitmap.SetPixels( pixels );
		return bitmap.ToPng();
	}

	static void ConvertPixels( Color[] source, Color[] output, int channel, double scale, bool invert, CancellationToken cancel )
	{
		for ( var i = 0; i < source.Length; i++ )
		{
			if ( i % 65536 == 0 ) cancel.ThrowIfCancellationRequested();
			var p = source[i];
			var value = (float)Math.Clamp( (channel switch { 0 => p.r, 1 => p.g, 2 => p.b, _ => p.a }) * scale, 0, 1 );
			if ( invert ) value = 1 - value;
			output[i] = new Color( value, value, value, 1 );
		}
	}
}
