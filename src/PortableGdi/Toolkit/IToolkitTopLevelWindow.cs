/*
 * IToolkitTopLevelWindow.cs - Implementation of the
 *			"System.Drawing.Toolkit.IToolkitTopLevelWindow" class.
 *
 * Copyright (C) 2003  Southern Storm Software, Pty Ltd.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

namespace System.Drawing.Toolkit
{
    [Flags]
    [NonStandardExtra]
    public enum ToolkitWindowFlags
    {
        Close			= (1 << 0),
        Minimize		= (1 << 1),
        Maximize		= (1 << 2),
        Caption			= (1 << 3),
        Border			= (1 << 4),
        ResizeHandles	= (1 << 5),
        Menu			= (1 << 6),
        Help			= (1 << 7),
        Resize			= (1 << 8),
        Move			= (1 << 9),
        TopMost			= (1 << 10),
        Modal			= (1 << 11),
        ShowInTaskbar	= (1 << 12),
        ToolWindow		= (1 << 13),
        Dialog			= (1 << 14),
        Default			= (Close | Minimize | Maximize | Caption |
                           Border | ResizeHandles | Menu | Resize |
                           Move | ShowInTaskbar)

    }; 
// This interface extends IToolkitWindow to include window manager functions.

    [NonStandardExtra]
    public interface IToolkitTopLevelWindow : IToolkitWindow
    {
        // Iconify the window.
        void Iconify();

        // Maximize the window.
        void Maximize();

        // Restore the window from its iconified or maximized state.
        void Restore();

        // Set the owner for modal and modeless dialog support.
        void SetDialogOwner(IToolkitTopLevelWindow owner);

        // Set this window's icon.
        void SetIcon(Icon icon);

        // Set this window's maximum size.
        void SetMaximumSize(Size size);

        // Set this window's minimum size.
        void SetMinimumSize(Size size);

        // Set the window title.
        void SetTitle(String title);

        // Change the set of supported window decorations and functions.
        void SetWindowFlags(ToolkitWindowFlags flags);

        // Set opacity
        void SetOpacity(double opacity);

    }; // interface IToolkitWindow

}; // namespace System.Drawing.Toolkit