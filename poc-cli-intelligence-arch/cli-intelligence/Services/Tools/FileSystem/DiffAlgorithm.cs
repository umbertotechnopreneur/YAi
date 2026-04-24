using System.Text;

namespace cli_intelligence.Services.Tools.FileSystem;

/// <summary>
/// Implements Myers' diff algorithm for generating unified diffs.
/// Based on "An O(ND) Difference Algorithm and Its Variations" by Eugene W. Myers.
/// </summary>
static class DiffAlgorithm
{
    /// <summary>
    /// Generates a unified diff between two texts using Myers' algorithm.
    /// </summary>
    public static string GenerateUnifiedDiff(string filename, string originalText, string modifiedText, int contextLines = 3)
    {
        var originalLines = originalText.Split('\n');
        var modifiedLines = modifiedText.Split('\n');

        var diff = ComputeDiff(originalLines, modifiedLines);
        return FormatUnifiedDiff(filename, originalLines, modifiedLines, diff, contextLines);
    }

    /// <summary>
    /// Computes the shortest edit script (SES) between two sequences using Myers' algorithm.
    /// </summary>
    private static List<DiffOperation> ComputeDiff(string[] original, string[] modified)
    {
        var n = original.Length;
        var m = modified.Length;
        var max = n + m;

        // V array stores the furthest reaching D-path for each diagonal k
        var v = new Dictionary<int, int>();
        v[1] = 0;

        // Trace stores the V arrays for each D value (for backtracking)
        var trace = new List<Dictionary<int, int>>();

        for (var d = 0; d <= max; d++)
        {
            trace.Add(new Dictionary<int, int>(v));

            for (var k = -d; k <= d; k += 2)
            {
                int x;

                // Choose whether to move down (insert) or right (delete)
                if (k == -d || (k != d && v.GetValueOrDefault(k - 1, -1) < v.GetValueOrDefault(k + 1, -1)))
                {
                    x = v.GetValueOrDefault(k + 1, 0);
                }
                else
                {
                    x = v.GetValueOrDefault(k - 1, 0) + 1;
                }

                var y = x - k;

                // Follow diagonal as far as possible (matching lines)
                while (x < n && y < m && original[x] == modified[y])
                {
                    x++;
                    y++;
                }

                v[k] = x;

                // Check if we've reached the end
                if (x >= n && y >= m)
                {
                    return Backtrack(trace, original, modified, n, m);
                }
            }
        }

        // Fallback: treat everything as delete + insert
        var operations = new List<DiffOperation>();
        for (var i = 0; i < original.Length; i++)
        {
            operations.Add(new DiffOperation(DiffOperationType.Delete, i, -1, original[i]));
        }
        for (var i = 0; i < modified.Length; i++)
        {
            operations.Add(new DiffOperation(DiffOperationType.Insert, -1, i, modified[i]));
        }
        return operations;
    }

    /// <summary>
    /// Backtracks through the trace to construct the diff operations.
    /// </summary>
    private static List<DiffOperation> Backtrack(List<Dictionary<int, int>> trace, string[] original, string[] modified, int n, int m)
    {
        var operations = new List<DiffOperation>();
        var x = n;
        var y = m;

        for (var d = trace.Count - 1; d >= 0; d--)
        {
            var v = trace[d];
            var k = x - y;

            int prevK;
            if (k == -d || (k != d && v.GetValueOrDefault(k - 1, -1) < v.GetValueOrDefault(k + 1, -1)))
            {
                prevK = k + 1;
            }
            else
            {
                prevK = k - 1;
            }

            var prevX = v.GetValueOrDefault(prevK, 0);
            var prevY = prevX - prevK;

            // Record diagonal moves (equal lines)
            while (x > prevX && y > prevY)
            {
                x--;
                y--;
                operations.Add(new DiffOperation(DiffOperationType.Equal, x, y, original[x]));
            }

            // Record horizontal move (delete) or vertical move (insert)
            if (d > 0)
            {
                if (x > prevX)
                {
                    x--;
                    operations.Add(new DiffOperation(DiffOperationType.Delete, x, -1, original[x]));
                }
                else if (y > prevY)
                {
                    y--;
                    operations.Add(new DiffOperation(DiffOperationType.Insert, -1, y, modified[y]));
                }
            }
        }

        operations.Reverse();
        return operations;
    }

    /// <summary>
    /// Formats the diff operations as a unified diff output.
    /// </summary>
    private static string FormatUnifiedDiff(string filename, string[] original, string[] modified, List<DiffOperation> operations, int contextLines)
    {
        var result = new StringBuilder();
        result.AppendLine($"--- {filename}");
        result.AppendLine($"+++ {filename}");

        var hunks = GroupIntoHunks(operations, contextLines);

        foreach (var hunk in hunks)
        {
            // Calculate hunk header
            var origStart = hunk.FirstOrDefault(o => o.OriginalIndex >= 0)?.OriginalIndex ?? 0;
            var origCount = hunk.Count(o => o.Type is DiffOperationType.Equal or DiffOperationType.Delete);
            var modStart = hunk.FirstOrDefault(o => o.ModifiedIndex >= 0)?.ModifiedIndex ?? 0;
            var modCount = hunk.Count(o => o.Type is DiffOperationType.Equal or DiffOperationType.Insert);

            result.AppendLine($"@@ -{origStart + 1},{origCount} +{modStart + 1},{modCount} @@");

            foreach (var op in hunk)
            {
                var prefix = op.Type switch
                {
                    DiffOperationType.Equal => " ",
                    DiffOperationType.Delete => "-",
                    DiffOperationType.Insert => "+",
                    _ => " "
                };
                result.AppendLine($"{prefix}{op.Line}");
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Groups diff operations into hunks with context lines.
    /// </summary>
    private static List<List<DiffOperation>> GroupIntoHunks(List<DiffOperation> operations, int contextLines)
    {
        var hunks = new List<List<DiffOperation>>();
        var currentHunk = new List<DiffOperation>();
        var equalCount = 0;

        foreach (var op in operations)
        {
            if (op.Type == DiffOperationType.Equal)
            {
                equalCount++;

                // If we have too many equal lines, close current hunk and start new one
                if (equalCount > contextLines * 2 && currentHunk.Count > 0)
                {
                    // Add trailing context
                    for (var i = 0; i < contextLines && currentHunk.Count - i > 0; i++)
                    {
                        if (operations.IndexOf(op) - equalCount + i < operations.Count)
                        {
                            currentHunk.Add(operations[operations.IndexOf(op) - equalCount + i]);
                        }
                    }

                    hunks.Add(currentHunk);
                    currentHunk = new List<DiffOperation>();

                    // Add leading context for next hunk
                    var skipCount = Math.Max(0, equalCount - contextLines);
                    for (var i = skipCount; i < equalCount; i++)
                    {
                        if (operations.IndexOf(op) - equalCount + i < operations.Count)
                        {
                            currentHunk.Add(operations[operations.IndexOf(op) - equalCount + i]);
                        }
                    }

                    equalCount = 0;
                }
                else
                {
                    currentHunk.Add(op);
                }
            }
            else
            {
                equalCount = 0;
                currentHunk.Add(op);
            }
        }

        if (currentHunk.Count > 0)
        {
            hunks.Add(currentHunk);
        }

        return hunks;
    }

    private sealed class DiffOperation
    {
        public DiffOperationType Type { get; }
        public int OriginalIndex { get; }
        public int ModifiedIndex { get; }
        public string Line { get; }

        public DiffOperation(DiffOperationType type, int originalIndex, int modifiedIndex, string line)
        {
            Type = type;
            OriginalIndex = originalIndex;
            ModifiedIndex = modifiedIndex;
            Line = line;
        }
    }

    private enum DiffOperationType
    {
        Equal,
        Delete,
        Insert
    }
}
