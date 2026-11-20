using System;
using System.IO;
using System.Threading;

namespace ImportUnityPackage;

/// <summary>Bounded retries for Windows sharing/access errors during directory finalization.</summary>
internal static class ImportStorage
{
	internal static bool IsTemporaryAccessError( Exception exception ) =>
		exception is IOException or UnauthorizedAccessException && (exception.HResult & 0xffff) is 5 or 32 or 33;

	internal static void Retry( Action operation, CancellationToken cancel, Action<int> retrying = null )
	{
		for ( var attempt = 0; ; attempt++ )
		{
			cancel.ThrowIfCancellationRequested();
			try { operation(); return; }
			catch ( Exception ex ) when ( IsTemporaryAccessError( ex ) && attempt < 6 )
			{
				retrying?.Invoke( attempt + 1 );
				// Maximum wait is 4.05 seconds. A cancellation interrupts the wait immediately.
				if ( cancel.WaitHandle.WaitOne( Math.Min( 150 << attempt, 1000 ) ) ) cancel.ThrowIfCancellationRequested();
			}
		}
	}
}
