using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emaily.BLL.Helpers.Interfaces
{
    public interface IEncryptionHelper
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }
}
