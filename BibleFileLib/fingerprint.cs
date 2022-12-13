using System;
using System.IO;
using System.Text;


namespace WordSend
{
	public class Fingerprint
	{
		const int HASHSIZE = 64;
		byte[] hash;
		Crypto.Sapphire sapp;
		string fingerprints;


		public Fingerprint()
		{
			hash = new byte[HASHSIZE];
			sapp = new Crypto.Sapphire();
		}

		public void Init()
		{
			sapp.hash_init();
		}

		public void HashString(string s)
        {
			int i;
			int c;
			for (i = 0; i < s.Length; i++)
            {
				c = (int)s[i];
				sapp.encrypt((byte)((c >> 8) & 0xFF));
				sapp.encrypt((byte)(c & 0xFF));
            }
        }

		public void HashDateTime(DateTime dt)
        {
			HashString(dt.ToString("o"));
        }

		public void HashBytes(byte[] b)
        {
			int i;
			for (i = 0; i < b.Length; i++)
            {
                sapp.encrypt(b[i]);
            }
        }

		public void HashFile(string fileName)
        {
            try
            {

                using (FileStream fsSource = new FileStream(fileName,
                    FileMode.Open, FileAccess.Read))
                {

                    // Read the source file into a byte array.
                    byte[] bytes = new byte[fsSource.Length];
                    int numBytesToRead = (int)fsSource.Length;
                    int numBytesRead = 0;
                    while (numBytesToRead > 0)
                    {
                        // Read may return anything from 0 to numBytesToRead.
                        int n = fsSource.Read(bytes, numBytesRead, numBytesToRead);

                        // Break when the end of the file is reached.
                        if (n == 0)
                            break;

                        numBytesRead += n;
                        numBytesToRead -= n;
                    }
                    HashBytes(bytes);
                }
            }
            catch (FileNotFoundException ioEx)
            {
                Logit.WriteLine("IO Exception in HashFile(" + fileName + "): "+ioEx.Message);
            }
            catch (Exception Ex)
            {
                Logit.WriteLine("Exception in HashFile("+fileName+"): "+Ex.Message);
            }
        }

		public void HashBool(bool f)
        {
			if (f)
				sapp.encrypt(1);
			else
				sapp.encrypt(0);
        }

		public string Finalize()
		{
			sapp.hash_final(hash, HASHSIZE);
			fingerprints = Convert.ToBase64String(hash);
			return fingerprints;
		}
	}
}