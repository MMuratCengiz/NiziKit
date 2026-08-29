// See https://aka.ms/new-console-template for more information

using NiziKit.Application;
using Pong;

Game.Run<PongGame>(new GameDesc
{
    Title = "Pong",
    Width = 1280,
    Height = 720,
    Resizable = true
});