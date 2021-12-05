/*
 * Copyright (C) 2016 Benjamin Kr�mer
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;

namespace LibAmiibo.Data.Settings.AppData.TitleID
{
    [Flags]
    public enum Category
    {
        Normal = 0x00,
        DlpChild = 0x01,
        Demo = 0x02,
        Contents = 0x03,
        AddOnContents = 0x04,
        Patch = 0x06,
        CannotExecution = 0x08,
        System = 0x10,
        RequireBatchUpdate = 0x20,
        NotRequireUserApproval = 0x40,
        NotRequireRightForMount = 0x80,
        CanSkipConvertJumpId = 0x100,
        TWL = 0x8000
    }
}