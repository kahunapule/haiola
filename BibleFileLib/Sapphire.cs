using System;
using System.IO;
using System.Security.Cryptography;

namespace Crypto
{
	/// <summary>
	/// Sapphire III stream cipher
	/// </summary>
	public sealed class Sapphire
	{
		private byte [] cards;
		private byte rotor,		            // Index that rotates smoothly
			ratchet,                // Index that moves erratically
			avalanche,              // Index heavily data dependent
			last_plain,             // Last plain text byte
			last_cipher;            // Last cipher text byte
		private byte rsum;					// Running sum
		private byte keypos;				// Index of current byte of user key.

		private byte keyrand(int limit, byte [] user_key)
		{
            unchecked
            {
                uint u,             // Value from 0 to limit to return.
                    retry_limiter,      // No infinite loops allowed.
                    mask;               // Select just enough bits.

                if (limit == 0) return 0;   // Avoid divide by zero error.
                retry_limiter = 0;
                mask = 1;               // Fill mask with enough bits to cover
                while (mask < limit)    // the desired range.
                    mask = (mask << 1) + 1;
                do
                {
                    rsum = (byte)(cards[rsum] + user_key[keypos++]);
                    if (keypos >= user_key.Length)
                    {
                        keypos = 0;            // Recycle the user key.
                        rsum = (byte)(rsum + user_key.Length);   // key "aaaa" != key "aaaaaaaa"
                    }
                    u = mask & rsum;
                    if (++retry_limiter > 11)
                        u = (byte)(u % limit);     // Prevent very rare long loops.
                }
                while (u > limit);
                return (byte)u;
            }
		}

		public void initialize(byte [] key)
		{
			// Key size may be up to 256 bytes.
			// Pass phrases may be used directly, with longer length
			// compensating for the low entropy expected in such keys.
			// Alternatively, shorter keys hashed from a pass phrase or
			// generated randomly may be used. For random keys, lengths
			// of from 4 to 16 bytes are recommended, depending on how
			// secure you want this to be.

			int i;
			byte toswap, swaptemp;

			// If we have been given no key, assume the default hash setup.
            unchecked
            {
                if (key.Length < 1)
                {
                    hash_init();
                    return;
                }

                // Start with cards all in order, one of each.

                cards = new Byte[256];
                for (i = 0; i < 256; i++)
                    cards[i] = (byte)i;

                // Swap the card at each position with some other card.

                toswap = 0;
                keypos = 0;         // Start with first byte of user key.
                rsum = 0;
                for (i = 255; i >= 0; i--)
                {
                    toswap = keyrand(i, key);
                    swaptemp = cards[i];
                    cards[i] = cards[toswap];
                    cards[toswap] = swaptemp;
                }

                // Initialize the indices and data dependencies.
                // Indices are set to different values instead of all 0
                // to reduce what is known about the state of the cards
                // when the first byte is emitted.

                rotor = cards[1];
                ratchet = cards[3];
                avalanche = cards[5];
                last_plain = cards[7];
                last_cipher = cards[rsum];

                toswap = swaptemp = rsum = 0;
                keypos = 0;
            }
		}

		public void hash_init()
		{
            unchecked
            {
                // This function is used to initialize non-keyed hash
                // computation.

                int i, j;

                // Initialize the indices and data dependencies.

                cards = new Byte[256];
                rotor = 1;
                ratchet = 3;
                avalanche = 5;
                last_plain = 7;
                last_cipher = 11;

                // Start with cards all in inverse order.

                for (i = 0, j = 255; i < 256; i++, j--)
                    cards[i] = (byte)j;
            }
		}

		public Sapphire()
		{
			hash_init();
		}

		public Sapphire(byte [] key)
		{
			initialize(key);
		}

		public byte encrypt(byte b)
		{
            unchecked
            {
                // Picture a single enigma rotor with 256 positions, rewired
                // on the fly by card-shuffling.

                // This cipher is a variant of one invented and written
                // by Kahunapule Michael Paul Johnson in November, 1993.

                byte swaptemp;

                // Shuffle the deck a little more.

                ratchet += cards[rotor++];
                swaptemp = cards[last_cipher];
                cards[last_cipher] = cards[ratchet];
                cards[ratchet] = cards[last_plain];
                cards[last_plain] = cards[rotor];
                cards[rotor] = swaptemp;
                avalanche += cards[swaptemp];

                // Output one byte from the state in such a way as to make it
                // very hard to figure out which one you are looking at.

                last_cipher = (byte)(b ^ cards[(cards[ratchet] + cards[rotor]) & 0xFF] ^
                    cards[cards[(cards[last_plain] +
                    cards[last_cipher] +
                    cards[avalanche]) & 0xFF]]);
                last_plain = b;
                return last_cipher;
            }
		}

		public byte decrypt(byte b)
		{
            unchecked
            {
                byte swaptemp;

                // Shuffle the deck a little more.

                ratchet += cards[rotor++];
                swaptemp = cards[last_cipher];
                cards[last_cipher] = cards[ratchet];
                cards[ratchet] = cards[last_plain];
                cards[last_plain] = cards[rotor];
                cards[rotor] = swaptemp;
                avalanche += cards[swaptemp];

                // Output one byte from the state in such a way as to make it
                // very hard to figure out which one you are looking at.

                last_plain = (byte)(b ^ cards[(cards[ratchet] + cards[rotor]) & 0xFF] ^
                    cards[cards[(cards[last_plain] +
                    cards[last_cipher] +
                    cards[avalanche]) & 0xFF]]);
                last_cipher = b;
                return last_plain;
            }
		}

		public byte [] hash_final(byte [] hash,      // Destination
            int hashlength) // Size of hash.
		{
			int i;

            unchecked
            {
                if (hash.Length < hashlength)
                    hashlength = hash.Length;
                for (i = 255; i >= 0; i--)
                    encrypt((byte)i);
                for (i = 0; i < hashlength; i++)
                    hash[i] = encrypt(0);
            }
			return hash;
		}
	}

	public class RNGPool
	{
		private RNGCryptoServiceProvider rng;
		private Sapphire StreamCipher;
		private byte [] pool;
		private const int poolsize = 512;
		private int poolpos;

		public void PoolLong(long L)
		{
			int i;

            unchecked
            {
                for (i = 0; i < 8; i++)
                {
                    if (poolpos >= poolsize)
                        poolpos = 0;
                    pool[poolpos++] = StreamCipher.encrypt((byte)(L & 0xFF));
                    L >>= 8;
                }
            }
		}

		public void PoolInt(int j)
		{
			int i;

            unchecked
            {
                for (i = 0; i < 8; i++)
                {
                    if (poolpos >= poolsize)
                        poolpos = 0;
                    pool[poolpos++] = StreamCipher.encrypt((byte)(j & 0xFF));
                    j >>= 8;
                }
            }
		}

		public RNGPool()
		{
			int i;
			FileStream f;

            unchecked
            {
                poolpos = 0;
                pool = new Byte[poolsize];
                rng = new RNGCryptoServiceProvider();
                rng.GetBytes(pool);
                byte[] key = new Byte[poolsize];
                string fileName = Environment.GetEnvironmentVariable("APPDATA") + "\\rng.bin";
                if (File.Exists(fileName))
                {
                    f = new FileStream(fileName, FileMode.Open);
                    f.Read(key, 0, poolsize);
                    f.Close();
                    StreamCipher = new Sapphire(key);
                    for (i = 0; i < poolsize; i++)
                    {
                        pool[i] ^= StreamCipher.encrypt(key[i]);
                    }
                }
                else
                {
                    StreamCipher = new Sapphire();
                    for (i = 0; i < poolsize; i++)
                        pool[i] ^= StreamCipher.encrypt(pool[i]);
                }

                PoolInt(Environment.TickCount);
                DateTime dt = DateTime.Now;
                PoolLong(dt.Ticks);
                for (i = 0; i < poolsize; i++)
                {
                    pool[i] ^= StreamCipher.encrypt(key[i]);
                }
                f = new FileStream(fileName, FileMode.Create);
                f.Write(pool, 0, poolsize);
                f.Close();
            }
		}

		public int GetInt()
		{
			int i;
			int Result;
			byte [] b;

            unchecked
            {
                b = new Byte[4];
                rng.GetBytes(b);
                Result = 0;
                for (i = 0; i < 4; i++)
                {
                    Result <<= 8;
                    if (poolpos >= poolsize)
                        poolpos = 0;
                    pool[poolpos] ^= b[i];
                    Result += StreamCipher.encrypt(pool[poolpos++]);
                }
                return Result;
            }
		}

		public double GetDouble()
		{
			int i;
			double Result;
			byte [] b;

            unchecked
            {
                b = new Byte[8];
                rng.GetBytes(b);
                Result = 0D;
                for (i = 0; i < 8; i++)
                {
                    if (poolpos >= poolsize)
                        poolpos = 0;
                    pool[poolpos] ^= b[i];
                    Result += StreamCipher.encrypt(pool[poolpos++]);
                    Result /= 256D;
                }
                return Result;
            }
		}

		public int GetInt(int limit)
		{
			int Result;

            unchecked
            {
                Result = (int)(GetDouble() * (double)limit);
                if (Result >= limit)
                    Result = limit - 1;	// Theoretically never executed.
                return Result;
            }
		}

		public void GetBytes(byte [] b)
		{
			int i;

            unchecked
            {
                rng.GetBytes(b);
                for (i = 0; i < b.Length; i++)
                {
                    if (poolpos >= poolsize)
                        poolpos = 0;
                    pool[poolpos] = StreamCipher.encrypt((byte)(pool[poolpos] ^ b[i]));
                    b[i] = StreamCipher.encrypt(pool[poolpos++]);
                }
            }
		}
	}
}
