﻿namespace GKCore.Interfaces
{
	public interface IChartWindow : IWorkWindow, ILocalization
	{
		IBaseWindow Base { get; }
		
		void GenChart(bool show);

		bool AllowPrint();
		void DoPrint();
		void DoPrintPreview();
	}
}
