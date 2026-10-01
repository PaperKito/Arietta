using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace Arietta.Features.GameLauncher;




public sealed class GamePost
{
    public string Title { get; set; } = "";

    
    public string Date { get; set; } = "";

    
    public string? Link { get; set; }
}





public sealed class GamePostGroup : ObservableObject
{
    public string Header { get; set; } = "";

    public List<GamePost> List { get; set; } = [];
}





public sealed class GameBannerItem
{
    public Uri? Image { get; set; }

    public string? Link { get; set; }
}
