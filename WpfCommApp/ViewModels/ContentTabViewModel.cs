using System;
using System.Collections.Generic;

namespace WpfCommApp
{
    public class ContentTabViewModel : ObservableObject
    {
        #region Fields

        private bool _visible;

        private IPageViewModel _current;

        #endregion

        #region Properties

        public IPageViewModel CurrentPage
        {
            get
            {
                return _current;
            }
            set
            {
                if (_current != value)
                {
                    PageHandling(value);
                    _current = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public string MeterSerialNo { get; }

        public string Name { get { return CurrentPage.Name + (MeterSerialNo == "" ? "" : " (" + MeterSerialNo + ")"); } }

        public List<IPageViewModel> Pages { get; }

        public bool Visible
        {
            get { return _visible; }
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    OnPropertyChanged(nameof(Visible));
                }
            }
        }

        #endregion

        #region Commands

        #endregion

        #region Constructor

        /// <summary>
        /// Creates an instance of ContentTabView used as the main tab control
        /// </summary>
        /// <param name="idx"></param>
        public ContentTabViewModel()
        {
            MeterSerialNo = "";
            Pages = new List<IPageViewModel>();
            Pages.Add(new ConnectViewModel());
            Pages.Add(new LoginViewModel());
            CurrentPage = Pages[0];
            Visible = true;
        }

        /// <summary>
        /// Creates an instance of the ContentTabView used to commission a meter.
        /// </summary>
        /// <param name="idx">Int used as index for Tab</param>
        /// <param name="serialIdx">Int used as index into serial comm objects</param>
        /// <param name="serial">String serial number of associated meter with this tab</param>
        public ContentTabViewModel(int serialIdx, string serial)
        {
            MeterSerialNo = serial;
            Pages = new List<IPageViewModel>();
            Pages.Add(new ConfigurationViewModel(serial));
            Pages.Add(new CommissioningViewModel(serial, serialIdx));
            Pages.Add(new ReviewViewModel(serial));
            CurrentPage = Pages[0];
            Visible = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initiates/Stops the polling of the Phase Diagnostic for the metter associated with this 
        /// tab instance, if the user is navigating to the Commissioning Page the polling is
        /// started and if the user is navigating away from the Commissioning Page then the polling
        /// is sent a termination signal.
        /// </summary>
        /// <param name="value"></param>
        private void PageHandling(object value)
        {
            if (value is CommissioningViewModel)
                (value as CommissioningViewModel).StartAsync.Execute(null);
            else if (_current is CommissioningViewModel)
                (_current as CommissioningViewModel).StopAsync.Execute(null);
        }

        #endregion
    }
}
