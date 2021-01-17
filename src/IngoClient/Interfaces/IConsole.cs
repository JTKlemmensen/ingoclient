using System;
using System.Collections.Generic;
using System.Text;

namespace IngoClient.Interfaces
{
    public interface IConsole
    {
        void WriteLine(string str);
        void Write<T>(T obj);
    }
}