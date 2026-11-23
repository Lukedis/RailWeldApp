using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RailWeldApp.Services;
using RailWeldApp.ViewModels;

namespace RailWeldApp.Views
{
    public partial class AdminReportExportView : UserControl
    {
        public AdminReportExportView()
        {
            InitializeComponent();
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminReportExportViewModel viewModel || viewModel.Report is null)
                return;

            var dialog = new PrintDialog();

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                dialog.PrintVisual(ReportPaper, $"RailWeld 质量报告 {viewModel.Report.ReportNo}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"打印失败：{ex.Message}",
                    "RailWeld",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminReportExportViewModel viewModel || viewModel.Report is null)
                return;

            var dialog = new SaveFileDialog
            {
                Title = "导出 PDF 报告",
                Filter = "PDF 文件 (*.pdf)|*.pdf",
                FileName = $"RailWeld-{viewModel.Report.ReportNo}.pdf",
                AddExtension = true,
                DefaultExt = ".pdf",
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                new ReportPdfService().Export(viewModel.Report, dialog.FileName);

                MessageBox.Show(
                    $"报告已导出：\n{dialog.FileName}",
                    "RailWeld",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"导出 PDF 失败：{ex.Message}",
                    "RailWeld",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}
