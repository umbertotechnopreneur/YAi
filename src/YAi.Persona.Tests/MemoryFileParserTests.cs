/*
 * YAi!
 *
 * Copyright © 2019-2026 UmbertoGiacobbiDotBiz. All rights reserved.
 * Website: https://umbertogiacobbi.biz
 * Email: hello@umbertogiacobbi.biz
 *
 * This file is part of YAi!.
 *
 * YAi! is free software: you can redistribute it and/or modify it under the terms
 * of the GNU Affero General Public License version 3 as published by the Free
 * Software Foundation.
 *
 * YAi! is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
 * without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR
 * PURPOSE. See the GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License along
 * with YAi!. If not, see <https://www.gnu.org/licenses/>.
 *
 * YAi.Persona.Tests
 * Unit tests for memory document parsing, serialization, and frontmatter updates
 */

#region Using directives

using YAi.Persona.Models;
using YAi.Persona.Services;

#endregion

namespace YAi.Persona.Tests;

/// <summary>
/// Tests for <see cref="MemoryFileParser"/> covering parsing, serialization, and frontmatter upserts.
/// </summary>
public sealed class MemoryFileParserTests
{
    [Fact]
    public void Parse_ReturnsEmptyDocument_WhenMarkdownIsEmpty ()
    {
        MemoryFileParser parser = new ();

        MemoryDocument document = parser.Parse (string.Empty);

        Assert.Empty (document.FrontMatter);
        Assert.Equal (string.Empty, document.Body);
    }

    [Fact]
    public void Parse_ReadsFrontMatterAndBody_WhenDocumentContainsClosedFrontMatter ()
    {
        MemoryFileParser parser = new ();

        string markdown = """
            ---
            priority: warm
            tags: [dotnet, tests]
            ---
            # Heading

            Body text.
            """;

        MemoryDocument document = parser.Parse (markdown);

        Assert.Equal ("warm", document.FrontMatter ["priority"]);
        Assert.Equal ("[dotnet, tests]", document.FrontMatter ["tags"]);
        Assert.Equal ("# Heading\n\nBody text.", document.Body);
    }

    [Fact]
    public void Parse_TreatsUnclosedFrontMatterAsDocumentWithoutBody ()
    {
        MemoryFileParser parser = new ();

        string markdown = """
            ---
            priority: warm
            tags: [dotnet, tests]
            body text that is never reached
            """;

        MemoryDocument document = parser.Parse (markdown);

        Assert.Equal ("warm", document.FrontMatter ["priority"]);
        Assert.Equal ("[dotnet, tests]", document.FrontMatter ["tags"]);
        Assert.Equal (string.Empty, document.Body);
    }

    [Fact]
    public void Serialize_OrdersFrontMatterKeysCaseInsensitively_AndNormalizesBodyLineEndings ()
    {
        MemoryFileParser parser = new ();
        MemoryDocument document = new ()
        {
            FrontMatter = new Dictionary<string, string>
            {
                ["zeta"] = "last",
                ["Alpha"] = "first",
                ["middle"] = "value"
            },
            Body = "line1\r\nline2"
        };

        string serialized = parser.Serialize (document);

        string newline = Environment.NewLine;
        string expected = $"---{newline}Alpha: first{newline}middle: value{newline}zeta: last{newline}---{newline}line1\nline2";
        Assert.Equal (expected, serialized);
    }

    [Fact]
    public void UpsertFrontMatter_AddsAndReplacesKeys_WhilePreservingBody ()
    {
        MemoryFileParser parser = new ();

        string markdown = """
            ---
            priority: hot
            scope: user
            ---
            Existing body.
            """;

        string updated = parser.UpsertFrontMatter (
            markdown,
            new Dictionary<string, string>
            {
                ["priority"] = "warm",
                ["language"] = "common"
            });

        MemoryDocument document = parser.Parse (updated);

        Assert.Equal ("warm", document.FrontMatter ["priority"]);
        Assert.Equal ("user", document.FrontMatter ["scope"]);
        Assert.Equal ("common", document.FrontMatter ["language"]);
        Assert.Equal ("Existing body.", document.Body);
    }
}