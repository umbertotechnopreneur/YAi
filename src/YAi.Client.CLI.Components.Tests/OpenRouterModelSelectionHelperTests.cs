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
 * YAi.Client.CLI.Components.Tests
 * Unit tests for OpenRouterModelSelectionHelper data-shaping logic
 */

#region Using directives

using System;
using System.Collections.Generic;
using System.Linq;
using YAi.Client.CLI.Components.Screens;
using YAi.Persona.Models;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="OpenRouterModelSelectionHelper"/> ordering, filtering, and cursor navigation.
/// </summary>
public sealed class OpenRouterModelSelectionHelperTests
{
    #region Helpers

    private static OpenRouterModel Model (string id, string name = "") =>
        new OpenRouterModel { Id = id, Name = name, Pricing = new OpenRouterPricing () };

    private static AppConfig DefaultConfig () =>
        new AppConfig
        {
            OpenRouter = new OpenRouterSection { Model = "openai/gpt-4o" }
        };

    #endregion

    #region BuildOrderedModels

    [Fact]
    public void BuildOrderedModels_Sorts_By_Provider_Then_Id ()
    {
        OpenRouterModelCatalog catalog = new OpenRouterModelCatalog
        {
            Data =
            [
                Model ("z-provider/b-model"),
                Model ("a-provider/z-model"),
                Model ("a-provider/a-model")
            ]
        };

        List<OpenRouterModel> ordered = OpenRouterModelSelectionHelper.BuildOrderedModels (catalog);

        Assert.Equal ("a-provider/a-model", ordered [0].Id);
        Assert.Equal ("a-provider/z-model", ordered [1].Id);
        Assert.Equal ("z-provider/b-model", ordered [2].Id);
    }

    [Fact]
    public void BuildOrderedModels_Excludes_Models_With_Empty_Id ()
    {
        OpenRouterModelCatalog catalog = new OpenRouterModelCatalog
        {
            Data =
            [
                Model (string.Empty),
                Model ("   "),
                Model ("openai/gpt-4o")
            ]
        };

        List<OpenRouterModel> ordered = OpenRouterModelSelectionHelper.BuildOrderedModels (catalog);

        Assert.Single (ordered);
        Assert.Equal ("openai/gpt-4o", ordered [0].Id);
    }

    [Fact]
    public void BuildOrderedModels_Empty_Catalog_Returns_Empty_List ()
    {
        OpenRouterModelCatalog catalog = new OpenRouterModelCatalog { Data = [] };
        List<OpenRouterModel> ordered = OpenRouterModelSelectionHelper.BuildOrderedModels (catalog);

        Assert.Empty (ordered);
    }

    #endregion

    #region FilterModels (via BuildViewState)

    [Fact]
    public void BuildViewState_Empty_Query_Shows_All_Models ()
    {
        List<OpenRouterModel> models = [Model ("openai/gpt-4o"), Model ("anthropic/claude")];
        OpenRouterModelSelectionViewState state =
            OpenRouterModelSelectionHelper.BuildViewState (models, null, DefaultConfig (), string.Empty, false, null);

        Assert.Equal (2, state.VisibleModels.Count);
    }

    [Fact]
    public void BuildViewState_Query_Filters_By_Id ()
    {
        List<OpenRouterModel> models =
        [
            Model ("openai/gpt-4o"),
            Model ("anthropic/claude"),
            Model ("openai/gpt-3.5")
        ];

        OpenRouterModelSelectionViewState state =
            OpenRouterModelSelectionHelper.BuildViewState (models, null, DefaultConfig (), "openai", false, null);

        Assert.Equal (2, state.VisibleModels.Count);
        Assert.All (state.VisibleModels, m => Assert.Contains ("openai", m.Id, StringComparison.Ordinal));
    }

    [Fact]
    public void BuildViewState_Query_Filters_By_Name ()
    {
        List<OpenRouterModel> models =
        [
            Model ("openai/gpt-4o", "GPT 4o"),
            Model ("anthropic/claude", "Claude 3")
        ];

        OpenRouterModelSelectionViewState state =
            OpenRouterModelSelectionHelper.BuildViewState (models, null, DefaultConfig (), "Claude", false, null);

        Assert.Single (state.VisibleModels);
    }

    [Fact]
    public void BuildViewState_No_Match_Returns_Empty_Visible_List ()
    {
        List<OpenRouterModel> models = [Model ("openai/gpt-4o")];
        OpenRouterModelSelectionViewState state =
            OpenRouterModelSelectionHelper.BuildViewState (models, null, DefaultConfig (), "zzz-no-match", false, null);

        Assert.Empty (state.VisibleModels);
    }

    #endregion

    #region GetCurrentSelection

    [Fact]
    public void GetCurrentSelection_Empty_List_Returns_Null ()
    {
        OpenRouterModel? result = OpenRouterModelSelectionHelper.GetCurrentSelection ([], 0);

        Assert.Null (result);
    }

    [Fact]
    public void GetCurrentSelection_Valid_Index_Returns_Model ()
    {
        List<OpenRouterModel> models = [Model ("a/b"), Model ("c/d")];
        OpenRouterModel? result = OpenRouterModelSelectionHelper.GetCurrentSelection (models, 1);

        Assert.Equal ("c/d", result?.Id);
    }

    [Fact]
    public void GetCurrentSelection_Out_Of_Range_Index_Returns_First ()
    {
        List<OpenRouterModel> models = [Model ("a/b"), Model ("c/d")];
        OpenRouterModel? result = OpenRouterModelSelectionHelper.GetCurrentSelection (models, 99);

        Assert.Equal ("a/b", result?.Id);
    }

    #endregion

    #region HandleCursorKey

    [Fact]
    public void HandleCursorKey_Down_Moves_One_Row_In_Two_Column_Grid ()
    {
        // Grid is 2 columns: index 0 (row 0 col 0), index 1 (row 0 col 1), index 2 (row 1 col 0)
        // Down from index 0 lands on index 2 (same column, next row)
        List<OpenRouterModel> models = [Model ("a/1"), Model ("b/2"), Model ("c/3"), Model ("d/4")];
        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey (models, 0, ConsoleKey.DownArrow);

        Assert.Equal (2, newIndex);
    }

    [Fact]
    public void HandleCursorKey_Up_Moves_One_Row_In_Two_Column_Grid ()
    {
        // Grid is 2 columns: down from 0 → 2, so up from 2 → 0
        List<OpenRouterModel> models = [Model ("a/1"), Model ("b/2"), Model ("c/3")];
        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey (models, 2, ConsoleKey.UpArrow);

        Assert.Equal (0, newIndex);
    }

    [Fact]
    public void HandleCursorKey_Down_At_Bottom_Row_Stays ()
    {
        // At the last row, Down does not move
        List<OpenRouterModel> models = [Model ("a/1"), Model ("b/2")];
        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey (models, 0, ConsoleKey.DownArrow);

        // Only one row; stays at 0
        Assert.Equal (0, newIndex);
    }

    [Fact]
    public void HandleCursorKey_Up_At_Top_Row_Stays ()
    {
        // At the first row, Up does not move
        List<OpenRouterModel> models = [Model ("a/1"), Model ("b/2"), Model ("c/3")];
        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey (models, 0, ConsoleKey.UpArrow);

        Assert.Equal (0, newIndex);
    }

    [Fact]
    public void HandleCursorKey_Empty_List_Returns_Zero ()
    {
        int newIndex = OpenRouterModelSelectionHelper.HandleCursorKey ([], 0, ConsoleKey.DownArrow);

        Assert.Equal (0, newIndex);
    }

    #endregion

    #region IsSearchCharacter and IsCursorKey

    [Fact]
    public void IsSearchCharacter_Printable_Char_Returns_True ()
    {
        ConsoleKeyInfo key = new ConsoleKeyInfo ('a', ConsoleKey.A, false, false, false);

        Assert.True (OpenRouterModelSelectionHelper.IsSearchCharacter (key));
    }

    [Fact]
    public void IsSearchCharacter_Control_Char_Returns_False ()
    {
        ConsoleKeyInfo key = new ConsoleKeyInfo ('\0', ConsoleKey.Backspace, false, false, false);

        Assert.False (OpenRouterModelSelectionHelper.IsSearchCharacter (key));
    }

    [Fact]
    public void IsCursorKey_Arrow_Keys_Return_True ()
    {
        Assert.True (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.UpArrow));
        Assert.True (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.DownArrow));
        Assert.True (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.LeftArrow));
        Assert.True (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.RightArrow));
    }

    [Fact]
    public void IsCursorKey_Non_Arrow_Returns_False ()
    {
        Assert.False (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.Enter));
        Assert.False (OpenRouterModelSelectionHelper.IsCursorKey (ConsoleKey.Escape));
    }

    #endregion
}
