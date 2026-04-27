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
 * YAi.Client.CLI.Components
 * YAi! colour palette expressed as a Terminal.Gui v2 Scheme
 */

#region Using directives

using Terminal.Gui.Drawing;

using TGuiAttr = Terminal.Gui.Drawing.Attribute;

#endregion

namespace YAi.Client.CLI.Components.Components;

/// <summary>
/// Provides the YAi! brand colour palette as a Terminal.Gui v2 <see cref="Scheme"/>.
/// </summary>
public static class YAiScheme
{
    #region Public properties

    /// <summary>Gets the default YAi! colour scheme for normal content areas.</summary>
    public static Scheme Default { get; } = BuildDefault ();

    /// <summary>Gets the YAi! colour scheme used for the header / banner area.</summary>
    public static Scheme Header { get; } = BuildHeader ();

    /// <summary>Gets the YAi! colour scheme used for the status bar.</summary>
    public static Scheme StatusBar { get; } = BuildStatusBar ();

    /// <summary>Gets the YAi! colour scheme used for warning / destructive actions.</summary>
    public static Scheme Warning { get; } = BuildWarning ();

    #endregion

    #region Private helpers

    private static readonly Color Background = new (12, 12, 20);
    private static readonly Color BodyText = new (200, 200, 210);
    private static readonly Color AccentCyan = new (0, 200, 255);
    private static readonly Color AccentGreen = new (0, 215, 135);
    private static readonly Color AccentOrange = new (255, 160, 40);
    private static readonly Color AccentRed = new (220, 60, 60);
    private static readonly Color Muted = new (110, 110, 130);
    private static readonly Color FocusBg = new (30, 30, 50);

    private static Scheme BuildDefault ()
    {
        return new Scheme
        {
            Normal = new TGuiAttr (BodyText, Background),
            Focus = new TGuiAttr (AccentCyan, FocusBg),
            HotNormal = new TGuiAttr (AccentGreen, Background),
            HotFocus = new TGuiAttr (AccentGreen, FocusBg),
            Disabled = new TGuiAttr (Muted, Background)
        };
    }

    private static Scheme BuildHeader ()
    {
        return new Scheme
        {
            Normal = new TGuiAttr (AccentCyan, Background),
            Focus = new TGuiAttr (AccentCyan, FocusBg),
            HotNormal = new TGuiAttr (AccentGreen, Background),
            HotFocus = new TGuiAttr (AccentGreen, FocusBg),
            Disabled = new TGuiAttr (Muted, Background)
        };
    }

    private static Scheme BuildStatusBar ()
    {
        return new Scheme
        {
            Normal = new TGuiAttr (Muted, Background),
            Focus = new TGuiAttr (BodyText, FocusBg),
            HotNormal = new TGuiAttr (AccentCyan, Background),
            HotFocus = new TGuiAttr (AccentCyan, FocusBg),
            Disabled = new TGuiAttr (Muted, Background)
        };
    }

    private static Scheme BuildWarning ()
    {
        return new Scheme
        {
            Normal = new TGuiAttr (AccentOrange, Background),
            Focus = new TGuiAttr (AccentRed, FocusBg),
            HotNormal = new TGuiAttr (AccentRed, Background),
            HotFocus = new TGuiAttr (AccentRed, FocusBg),
            Disabled = new TGuiAttr (Muted, Background)
        };
    }

    #endregion
}
