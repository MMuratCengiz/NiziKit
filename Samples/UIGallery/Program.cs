using UIGallery;

if (UiDebug.Run(args, () =>
    {
        var gallery = new Gallery(() => null, PatternTexture.Create(false));
        UiDebug.OnFrame += _ => gallery.Pump();
        return gallery.Root;
    }))
{
    return;
}

Game.Run(() => new GalleryGame());
