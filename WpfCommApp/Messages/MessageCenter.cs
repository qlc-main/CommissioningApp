
namespace WpfCommApp
{
    /// <summary>
    /// Message passing interface to send messages from IPageViewModels to MainViewModel
    /// </summary>
    public class MessageCenter
    {
        #region Fields

        #endregion

        #region Properties

        public object Args { get; }

        public string Command { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Basic message indicating to move forward to the next page
        /// </summary>
        public MessageCenter()
            : this("")
        {
        }

        /// <summary>
        /// Advanced message that will use the command parameter to
        /// determine the MainViewMode's action
        /// </summary>
        /// <param name="command"></param>
        public MessageCenter(string command)
        {
            Command = command;
        }

        /// <summary>
        /// Advanced messaged that will use the command parameter to
        /// determine the MainViewModel's action as well as use the
        /// included arguments in the function call
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public MessageCenter(string command, object args)
        {
            Command = command;
            Args = args;
        }

        #endregion

    }
}
