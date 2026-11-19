// See https://aka.ms/new-console-template for more information

using NiziKit.Application;
using Pong;

Game.Run(() => new PongGame(new GameDesc
{
    Title = "Pong",
    Width = 450,
    Height = 800,
    Resizable = true
}));