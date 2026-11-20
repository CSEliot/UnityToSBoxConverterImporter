using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ImportUnityPackage;

/// <summary>Short completion text, with detailed warnings and errors retained in the report.</summary>
public sealed record ImportCompletion( int AssetCount, string[] Warnings, string[] Errors, bool Cancelled )
{
	/// <summary>Every raw error line. The report JSON keeps these; <see cref="Errors"/> shows grouped display lines instead of hundreds of repeats.</summary>
	public string[] DetailedErrors { get; init; } = Array.Empty<string>();

	public string Title => Cancelled ? "Import preparation cancelled" : Errors.Length > 0 ? "Import completed with errors" : "Import complete";
	public string Counts => DetailedErrors.Length > Errors.Length
		? $"Warnings: {Warnings.Length} · Errors: {DetailedErrors.Length} ({Errors.Length} group{(Errors.Length == 1 ? "" : "s")})"
		: $"Warnings: {Warnings.Length} · Errors: {Errors.Length}";
	public string Message => Cancelled ? $"Imported files kept. Resource preparation stopped.\n{Counts}" : $"{AssetCount} assets imported.\n{Counts}";
	public string Status => $"{(Errors.Length == 0 && !Cancelled ? "✓ " : "")}{Title}\n{Message}";

	public static ImportCompletion Create( ImportResult result, IEnumerable<string> preparationWarnings, IEnumerable<string> preparationErrors, bool cancelled )
	{
		var errors = result.Unresolved.Where( i => i.Severity == "Error" ).Select( i => $"{i.Asset}: {i.Message}" )
			.Concat( preparationErrors ?? Array.Empty<string>() ).Distinct().ToArray();
		var warnings = result.Warnings.Concat( result.Unresolved.Where( i => i.Severity == "Warning" ).Select( i => $"{i.Asset}: {i.Message}" ) )
			.Concat( preparationWarnings ?? Array.Empty<string>() ).Except( errors ).Distinct().ToArray();
		return new( result.AssetCount, warnings, GroupErrors( errors ), cancelled ) { DetailedErrors = errors };
	}

	/// <summary>Collapse errors that repeat the same message (e.g. hundreds of identical resource compiler failures) into one line with a count and the first paths.</summary>
	static string[] GroupErrors( string[] errors )
	{
		var lines = new List<string>();
		foreach ( var group in errors.GroupBy( e => Split( e ).Message ))
		{
			var items = group.ToArray();
			if ( items.Length == 1 ) { lines.Add( items[0] ); continue; }
			var paths = items.Select( e => Split( e ).Path ).Where( p => p.Length > 0 ).ToArray();
			lines.Add( $"{items.Length} resources: {Split( items[0] ).Message}" + (paths.Length == 0 ? "" : $" — {string.Join( ", ", paths.Take( 10 ) )}") +
				(paths.Length > 10 ? $" (+{paths.Length - 10} more; full list in the report)" : "") );
		}
		return lines.ToArray();
	}

	static (string Path, string Message) Split( string error )
	{
		var i = error.IndexOf( ": ", StringComparison.Ordinal );
		return i < 0 ? ("", error) : (error[..i], error[(i + 2)..]);
	}

	public string AppendReport( string json )
	{
		if ( string.IsNullOrWhiteSpace( json ) ) json = "{}";
		var report = JsonNode.Parse( json );
		report["Completion"] = JsonSerializer.SerializeToNode( new { Title, AssetCount, Cancelled, Warnings, Errors = DetailedErrors.Length > 0 ? DetailedErrors : Errors } );
		return report.ToJsonString( new JsonSerializerOptions { WriteIndented = true } );
	}
}
