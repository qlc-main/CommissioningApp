namespace WpfCommApp
{
    public interface ITab
    {
        string Name { get; }
        IPageViewModel CurrentPage { get; set; }
        bool Visible { get; }
    }
}
