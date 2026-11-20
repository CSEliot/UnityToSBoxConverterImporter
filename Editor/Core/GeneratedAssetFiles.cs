using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace ImportUnityPackage;

/// <summary>Names derived assets beside their sources, reusing matching content without an on-disk index.</summary>
internal sealed class GeneratedAssetFiles
{
	readonly string scratch;
	readonly string destination;
	readonly HashSet<string> reserved;
	readonly List<(string Token, string Preferred, string File)> pending = new();
	readonly Dictionary<string, string> resolved = new();
	readonly HashSet<string> assigned = new( StringComparer.OrdinalIgnoreCase );
	readonly Dictionary<string, Dictionary<int, string>> existing = new( StringComparer.OrdinalIgnoreCase );

	public GeneratedAssetFiles( ImportPlan plan, string stage, string destination )
	{
		this.destination = destination;
		scratch = Path.Combine( Path.GetDirectoryName( stage ), "generated" );
		reserved = plan.Archive.Assets.Select( a => a.Path ).ToHashSet( StringComparer.OrdinalIgnoreCase );
		foreach ( var item in plan.Assets )
		{
			if ( item.Vmat ) reserved.Add( Path.ChangeExtension( item.Asset.Path, ".vmat" ) );
			if ( item.Tmat ) reserved.Add( Path.ChangeExtension( item.Asset.Path, ".tmat" ) );
			if ( item.Vmdl ) reserved.Add( Path.ChangeExtension( item.Asset.Path, ".vmdl" ) );
			if ( UnityImport.NeedsTextureResource( item.Asset.Path ) ) reserved.Add( item.Asset.Path + ".vtex" );
		}
	}

	public (string Reference, string File) Add( string source, string purpose, string extension )
	{
		Directory.CreateDirectory( scratch );
		var token = $"iup:generated:{pending.Count}";
		var file = Path.Combine( scratch, pending.Count + ".iup" );
		var preferred = Path.ChangeExtension( source, null ) + "_iup_" + purpose + extension;
		pending.Add( (token, preferred, file) );
		return (token, file);
	}

	public void Complete( Func<string, string> output, CancellationToken cancel )
	{
		foreach ( var item in pending )
		{
			cancel.ThrowIfCancellationRequested();
			var hash = ImportMergePlan.HashFile( item.File, cancel ) ?? throw new IOException( "Generated asset is missing: " + item.Preferred );
			var candidates = Existing( item.Preferred, cancel );
			string Name( int number ) => number == 0 ? item.Preferred : Path.ChangeExtension( item.Preferred, null ) + "_" + number + Path.GetExtension( item.Preferred );
			// Look for matching numbered variants even when an earlier suffix has been deleted.
			var match = candidates.Where( p => p.Value == hash ).OrderBy( p => p.Key ).Select( p => Name( p.Key ) )
				.FirstOrDefault( path => !reserved.Contains( path ) );
			var chosen = match;
			if ( chosen == null )
			{
				var number = 0;
				while ( true )
				{
					cancel.ThrowIfCancellationRequested();
					chosen = Name( number );
					if ( !reserved.Contains( chosen ) && !candidates.ContainsKey( number ) && !Directory.Exists( UnityImport.DiskPath( destination, chosen ) ) ) break;
					number++;
				}
				candidates[number] = hash;
			}
			resolved[item.Token] = UnityImport.DocumentPath( "Imported/" + chosen );
			if ( assigned.Add( chosen ) ) File.Move( item.File, output( chosen ) );
			else File.Delete( item.File );
		}
	}

	Dictionary<int, string> Existing( string preferred, CancellationToken cancel )
	{
		if ( existing.TryGetValue( preferred, out var result ) ) return result;
		result = new();
		var directory = Path.GetDirectoryName( UnityImport.DiskPath( destination, preferred ) );
		UnityImport.CheckDirectory( directory );
		if ( Directory.Exists( directory ) )
		{
			var pattern = new Regex( "^" + Regex.Escape( Path.GetFileNameWithoutExtension( preferred ) ) + @"(?:_([1-9][0-9]*))?" + Regex.Escape( Path.GetExtension( preferred ) ) + "$", RegexOptions.IgnoreCase );
			foreach ( var file in Directory.EnumerateFiles( directory ) )
			{
				cancel.ThrowIfCancellationRequested();
				var number = Number( file, pattern );
				if ( number < 0 ) continue;
				UnityImport.CheckDirectory( file );
				result[number] = ImportMergePlan.HashFile( file, cancel );
			}
		}
		existing[preferred] = result;
		return result;
	}

	static int Number( string path, Regex pattern )
	{
		var match = pattern.Match( Path.GetFileName( path ) );
		if ( !match.Success ) return -1;
		return !match.Groups[1].Success ? 0 : int.TryParse( match.Groups[1].Value, out var number ) ? number : -1;
	}

	public string ResolveDocument( string text ) => Regex.Replace( text, "\"iup:generated:[0-9]+\"", m => UnityMaterial.Quote( resolved[m.Value[1..^1]] ) );
}
