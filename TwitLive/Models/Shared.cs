
using CommunityToolkit.Mvvm.ComponentModel;
using SQLite;

namespace TwitLive.Models;

public partial class Shared: ObservableObject
{
    [PrimaryKey, AutoIncrement, Column("Id")]
    public int Id { get; set; }
    [ObservableProperty]
    private string _title = string.Empty;
    [ObservableProperty]
    private string _description = string.Empty;
    [ObservableProperty]
    private string _link = string.Empty;
    [ObservableProperty]
    private string _image = string.Empty;
    [ObservableProperty]
    private string _url = string.Empty;
    [ObservableProperty]
    private DateTime _pubDate = DateTime.Now;
}
