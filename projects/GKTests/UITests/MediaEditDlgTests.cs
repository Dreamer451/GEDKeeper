﻿/*
 *  "GEDKeeper", the personal genealogical database editor.
 *  Copyright (C) 2009-2016 by Serg V. Zhdanovskih (aka Alchemist, aka Norseman).
 *
 *  This file is part of "GEDKeeper".
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#if !__MonoCS__

using System;
using GKCommon.GEDCOM;
using GKCore.Interfaces;
using GKTests.Mocks;
using GKUI.Dialogs;
using NUnit.Extensions.Forms;
using NUnit.Framework;

namespace GKTests.UITests
{
    /// <summary>
    /// 
    /// </summary>
    [TestFixture]
    public class MediaEditDlgTests : NUnitFormTest
    {
        public MediaEditDlgTests()
        {
        }

        private IBaseContext fContext;
        private GEDCOMMultimediaRecord fMultimediaRecord;
        private IBaseWindow fBase;

        private MediaEditDlg _frm;

        public override void Setup()
        {
            base.Setup();

            fBase = new BaseWindowMock();
            fContext = fBase.Context;
            fMultimediaRecord = new GEDCOMMultimediaRecord(fContext.Tree, fContext.Tree, "", "");

            //ExpectModal("MediaEditDlg", "DlgHandler");
            _frm = new MediaEditDlg(fBase);
            _frm.MediaRec = fMultimediaRecord;
            //_frm.ShowDialog();
            //_frm.Show();
        }

        [Test]
        public void Test_Misc()
        {
            Assert.AreEqual(fBase, _frm.Base);
            Assert.AreEqual(fMultimediaRecord, _frm.MediaRec);
        }

        public void DlgHandler()
        {
            //var btnCancel = new ButtonTester("btnCancel", "NoteEditDlg");
            //btnCancel.Click();
        }
    }
}

#endif
