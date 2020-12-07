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
                    if (_current != null)
                        Previous = _current;
                    PageHandling(value);
                    _current = value;
                    OnPropertyChanged(nameof(CurrentPage));
                }
            }
        }

        public string MeterSerialNo { get; }

        public string Name 
        { 
            get 
            {
                if (CurrentPage != null)
                    return CurrentPage.Name + (MeterSerialNo == "" ? "" : $" ({MeterSerialNo} on {SerialIdx})");
                else
                    return Previous.Name + (MeterSerialNo == "" ? "" : $" ({MeterSerialNo} on {SerialIdx})");
            } 
        }

        public List<IPageViewModel> Pages { get; }

        public IPageViewModel Previous { get; private set; }

        public string SerialIdx;

        public Tuple<string, string> DataTuple { get { return new Tuple<string, string>(SerialIdx, MeterSerialNo); } }

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
        public ContentTabViewModel(string serialIdx, string serial)
        {
            MeterSerialNo = serial;
            SerialIdx = serialIdx;
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
        /// Initiates/Stops the async processes for each meter associated with this 
        /// tab instance, if the user is navigating to the Commissioning Page the polling is
        /// started and if the user is navigating away from the Commissioning Page then the polling
        /// is sent a termination signal.
        /// </summary>
        /// <param name="value"></param>
        private void PageHandling(object value)
        {
            if (value is ConfigurationViewModel)
                (value as ConfigurationViewModel).StartAsync.Execute(null);
            else if (_current is ConfigurationViewModel)
                (_current as ConfigurationViewModel).StopAsync.Execute(null);

            if (value is CommissioningViewModel)
                (value as CommissioningViewModel).StartAsync.Execute(null);
            else if (_current is CommissioningViewModel)
                (_current as CommissioningViewModel).StopAsync.Execute(null);

            if (value is ReviewViewModel)
                (value as ReviewViewModel).StartAsync.Execute(null);
            else if (_current is ReviewViewModel)
                (_current as ReviewViewModel).StopAsync.Execute(null);
        }

        #endregion
    }
}
