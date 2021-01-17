using IngoClient.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IngoClient.Interfaces
{
    public interface IIngoClient
    {
        void Login(User user);
        void Confirm(string code);
        void RefreshToken(User user);
        string GenerateGameToken(User user);
        GameInfo GetGameSession();
        void Play(User user, GameInfo info);
    }
}