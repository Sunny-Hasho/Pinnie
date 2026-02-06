using System;
using System.Windows;
using System.Windows.Interop;
using Pinnie.ViewModels;

namespace Pinnie
{
    public partial class MainWindow : Window
    {
        private readonly AppViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new AppViewModel();
            this.DataContext = _viewModel;
            
            // Force HWND creation to trigger OnSourceInitialized
            var helper = new WindowInteropHelper(this);
            helper.EnsureHandle();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            _viewModel.Initialize(helper.Handle);

            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            _viewModel.ProcessMessage(msg, wParam);
            return IntPtr.Zero;
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
