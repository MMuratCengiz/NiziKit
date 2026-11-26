using UIGallery;

if (UiDebug.Run(args, () => new Gallery(() => null, PatternTexture.Create(false)).Root))
{
    return;
}

Game.Run(() => new GalleryGame());
