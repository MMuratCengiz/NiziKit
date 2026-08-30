// See https://aka.ms/new-console-template for more information

using NiziKit.Application;
using Pong;

Game.Run(() => new PongGame(new GameDesc
{
    Title = "Pong",
    Width = 720,
    Height = 1280,
    Resizable = true
}));