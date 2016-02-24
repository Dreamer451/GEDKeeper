﻿using System;
using System.Drawing;
using System.Windows.Forms;

using GKCommon.Controls;
using GKCommon.GEDCOM;
using GKCore;
using GKCore.Interfaces;

namespace GKUI.Controls
{
	/// <summary>
	/// 
	/// </summary>
	public partial class GKMergeControl : UserControl
	{
		private GEDCOMRecord fRec1;
		private GEDCOMRecord fRec2;

		private readonly HyperView fView1;
		private readonly HyperView fView2;

		private IBaseWindow fBase;
		private GEDCOMTree fTree;
		private GEDCOMRecordType fMergeMode;

		public IBaseWindow Base
		{
			get	{
				return this.fBase;
			}
			set
			{
			    this.fBase = value;
			    this.fTree = (value != null) ? this.fBase.Tree : null;
			}
		}

		public GEDCOMRecordType MergeMode
		{
			get { return this.fMergeMode; }
			set { this.fMergeMode = value; }
		}

		public GEDCOMRecord Rec1
		{
			get { return this.fRec1; }
		}

		public GEDCOMRecord Rec2
		{
			get { return this.fRec2; }
		}

		public GKMergeControl()
		{
			InitializeComponent();

			this.fView1 = new HyperView();
			this.fView1.Location = new Point(8, 56);
			this.fView1.Size = new Size(329, 248);
			this.Controls.Add(this.fView1);

			this.fView2 = new HyperView();
			this.fView2.Location = new Point(344, 56);
			this.fView2.Size = new Size(329, 248);
			this.Controls.Add(this.fView2);

			this.SetRec1(null);
			this.SetRec2(null);

			this.btnRec1Select.Text = LangMan.LS(LSID.LSID_DlgSelect) + @"...";
			this.btnRec2Select.Text = LangMan.LS(LSID.LSID_DlgSelect) + @"...";
		}


		private void RecordMerge(GEDCOMRecord targetRec, GEDCOMRecord sourceRec)
		{
            if (targetRec == null)
            {
                throw new ArgumentNullException("targetRec");
            }

            if (sourceRec == null)
            {
                throw new ArgumentNullException("sourceRec");
            }

            XRefReplacer repMap = new XRefReplacer();
			try
			{
				repMap.AddXRef(sourceRec, sourceRec.XRef, targetRec.XRef);

				int num = this.fTree.RecordsCount;
				for (int i = 0; i < num; i++) {
					this.fTree[i].ReplaceXRefs(repMap);
				}

				switch (targetRec.RecordType)
				{
					case GEDCOMRecordType.rtIndividual:
                        GEDCOMIndividualRecord indRec = (GEDCOMIndividualRecord)sourceRec;
                        indRec.MoveTo(targetRec, false);
                        this.Base.RecordDelete(indRec, false);
						break;

					case GEDCOMRecordType.rtNote:
                        GEDCOMNoteRecord noteRec = (GEDCOMNoteRecord)sourceRec;
                        noteRec.MoveTo(targetRec, false);
                        this.Base.RecordDelete(noteRec, false);
						break;

					case GEDCOMRecordType.rtFamily:
                        GEDCOMFamilyRecord famRec = (GEDCOMFamilyRecord)sourceRec;
                        famRec.MoveTo(targetRec, false);
                        this.Base.RecordDelete(famRec, false);
						break;

					case GEDCOMRecordType.rtSource:
                        GEDCOMSourceRecord srcRec = (GEDCOMSourceRecord)sourceRec;
                        srcRec.MoveTo(targetRec, false);
                        this.Base.RecordDelete(srcRec, false);
						break;
				}

				this.Base.ChangeRecord(targetRec);

				this.Base.RefreshLists(false);
			}
			finally
			{
                repMap.Dispose();
			}
		}

		private void UpdateMergeButtons()
		{
			this.btnMergeToLeft.Enabled = (this.fRec1 != null && this.fRec2 != null);
			this.btnMergeToRight.Enabled = (this.fRec1 != null && this.fRec2 != null);
		}

		public void SetRec1(GEDCOMRecord value)
		{
			this.fRec1 = value;
			this.UpdateMergeButtons();

			if (this.fRec1 == null)
			{
				this.Lab1.Text = @"XXX1";
				this.Edit1.Text = "";
				this.fView1.Lines.Clear();
			}
			else
			{
				this.Lab1.Text = this.fRec1.XRef;

				switch (this.fMergeMode)
                {
					case GEDCOMRecordType.rtIndividual:
						{
                            GEDCOMIndividualRecord iRec = (GEDCOMIndividualRecord)this.fRec1;
							this.Edit1.Text = iRec.GetNameString(true, false);
							GKUtils.ShowPersonInfo(iRec, this.fView1.Lines, this.fBase.ShieldState);
							break;
						}
					case GEDCOMRecordType.rtNote:
						{
                            GEDCOMNoteRecord nRec = (GEDCOMNoteRecord)this.fRec1;
							this.Edit1.Text = nRec.Note[0];
                            GKUtils.ShowNoteInfo(nRec, this.fView1.Lines);
							break;
						}
					case GEDCOMRecordType.rtFamily:
						{
                            GEDCOMFamilyRecord famRec = (GEDCOMFamilyRecord)this.fRec1;
							this.Edit1.Text = GKUtils.GetFamilyString(famRec);
                            GKUtils.ShowFamilyInfo(famRec, this.fView1.Lines, this.fBase.ShieldState);
							break;
						}
					case GEDCOMRecordType.rtSource:
						{
                            GEDCOMSourceRecord srcRec = (GEDCOMSourceRecord)this.fRec1;
							this.Edit1.Text = srcRec.FiledByEntry;
                            GKUtils.ShowSourceInfo(srcRec, this.fView1.Lines);
							break;
						}
				}
			}
		}

		public void SetRec2(GEDCOMRecord value)
		{
			this.fRec2 = value;
			this.UpdateMergeButtons();

			if (this.fRec2 == null)
			{
				this.Lab2.Text = @"XXX2";
				this.Edit2.Text = "";
				this.fView2.Lines.Clear();
			}
			else
			{
				this.Lab2.Text = this.fRec2.XRef;

				switch (this.fMergeMode)
                {
					case GEDCOMRecordType.rtIndividual:
						{
                            GEDCOMIndividualRecord iRec = (GEDCOMIndividualRecord)this.fRec2;
							this.Edit2.Text = iRec.GetNameString(true, false);
                            GKUtils.ShowPersonInfo(iRec, this.fView2.Lines, this.fBase.ShieldState);
							break;
						}
					case GEDCOMRecordType.rtNote:
						{
                            GEDCOMNoteRecord nRec = (GEDCOMNoteRecord)this.fRec2;
							this.Edit2.Text = nRec.Note[0];
                            GKUtils.ShowNoteInfo(nRec, this.fView2.Lines);
							break;
						}
					case GEDCOMRecordType.rtFamily:
						{
                            GEDCOMFamilyRecord famRec = (GEDCOMFamilyRecord)this.fRec2;
							this.Edit2.Text = GKUtils.GetFamilyString(famRec);
                            GKUtils.ShowFamilyInfo(famRec, this.fView2.Lines, this.fBase.ShieldState);
							break;
						}
					case GEDCOMRecordType.rtSource:
						{
                            GEDCOMSourceRecord srcRec = (GEDCOMSourceRecord)this.fRec2;
							this.Edit2.Text = srcRec.FiledByEntry;
                            GKUtils.ShowSourceInfo(srcRec, this.fView2.Lines);
							break;
						}
				}
			}
		}

		void btnRec1Select_Click(object sender, EventArgs e)
		{
			GEDCOMRecord irec = this.Base.SelectRecord(this.fMergeMode, null);
			if (irec != null) this.SetRec1(irec);
		}

		void btnRec2Select_Click(object sender, EventArgs e)
		{
			GEDCOMRecord irec = this.Base.SelectRecord(this.fMergeMode, null);
			if (irec != null) this.SetRec2(irec);
		}

		void btnMergeToLeft_Click(object sender, EventArgs e)
		{
			this.RecordMerge(this.fRec1, this.fRec2);
			this.SetRec1(this.fRec1);
			this.SetRec2(null);
		}

		void btnMergeToRight_Click(object sender, EventArgs e)
		{
			this.RecordMerge(this.fRec2, this.fRec1);
			this.SetRec1(null);
			this.SetRec2(this.fRec2);
		}

	}
}
