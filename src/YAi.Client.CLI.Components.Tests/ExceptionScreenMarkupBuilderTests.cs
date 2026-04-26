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
 * Unit tests for ExceptionScreenMarkupBuilder markup output
 */

#region Using directives

using System;
using YAi.Client.CLI.Components.Screens;

#endregion

namespace YAi.Client.CLI.Components.Tests;

/// <summary>
/// Tests for <see cref="ExceptionScreenMarkupBuilder"/> markup generation.
/// </summary>
public sealed class ExceptionScreenMarkupBuilderTests
{
    #region Basic output

    [Fact]
    public void BuildMarkup_Contains_Exception_Type ()
    {
        Exception exception = new InvalidOperationException ("test message");
        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        Assert.Contains ("InvalidOperationException", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Contains_Escaped_Message ()
    {
        Exception exception = new Exception ("a [dangerous] message");
        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        // Markup.Escape converts [ to [[ — verify the double-bracket escape is present
        Assert.Contains ("[[dangerous]]", markup, StringComparison.Ordinal);
        Assert.Contains ("dangerous", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Contains_HResult ()
    {
        Exception exception = new InvalidOperationException ("oops");
        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        Assert.Contains ("HResult", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Data dictionary

    [Fact]
    public void BuildMarkup_With_Data_Shows_Key_Value_Pairs ()
    {
        Exception exception = new Exception ("with data");
        exception.Data["key1"] = "value1";
        exception.Data["key2"] = 42;

        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        Assert.Contains ("key1", markup, StringComparison.Ordinal);
        Assert.Contains ("value1", markup, StringComparison.Ordinal);
        Assert.Contains ("key2", markup, StringComparison.Ordinal);
        Assert.Contains ("42", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Without_Data_Does_Not_Show_Data_Section ()
    {
        Exception exception = new Exception ("no data");
        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        // The "Data:" label should only appear when there are entries
        Assert.DoesNotContain ("Data:", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Inner exception

    [Fact]
    public void BuildMarkup_With_Inner_Exception_Shows_Both_Types ()
    {
        Exception inner = new ArgumentNullException ("param1");
        Exception outer = new InvalidOperationException ("outer message", inner);

        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (outer);

        Assert.Contains ("InvalidOperationException", markup, StringComparison.Ordinal);
        Assert.Contains ("ArgumentNullException", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Inner_Exception_Shows_Inner_Exception_Label ()
    {
        Exception inner = new Exception ("inner");
        Exception outer = new Exception ("outer", inner);

        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (outer);

        Assert.Contains ("Inner exception", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Nested_Inner_Exception_Renders_All_Levels ()
    {
        Exception level3 = new NotSupportedException ("level 3");
        Exception level2 = new InvalidCastException ("level 2", level3);
        Exception level1 = new InvalidOperationException ("level 1", level2);

        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (level1);

        Assert.Contains ("NotSupportedException", markup, StringComparison.Ordinal);
        Assert.Contains ("InvalidCastException", markup, StringComparison.Ordinal);
        Assert.Contains ("InvalidOperationException", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Color coding

    [Fact]
    public void BuildMarkup_Top_Level_Uses_Red_Color ()
    {
        Exception exception = new Exception ("top");
        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (exception);

        Assert.Contains ("red", markup, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildMarkup_Inner_Exception_Uses_Orange_Color ()
    {
        Exception inner = new Exception ("inner");
        Exception outer = new Exception ("outer", inner);

        string markup = ExceptionScreenMarkupBuilder.BuildMarkup (outer);

        Assert.Contains ("orange1", markup, StringComparison.Ordinal);
    }

    #endregion

    #region Null guard

    [Fact]
    public void BuildMarkup_Null_Exception_Throws_ArgumentNullException ()
    {
        Assert.Throws<ArgumentNullException> (() => ExceptionScreenMarkupBuilder.BuildMarkup (null!));
    }

    #endregion
}
