using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WordSend
{
    /// <summary>
    /// Object to put in generic list to use to find book names in string.
    /// Sorting puts longer names first so that "1 John" won't match "John".
    /// </summary>
    public class bookMatch : IComparable<bookMatch>
    {
        public string bookName; // Bible book short name or abbreviation
        public string bookTLA;  // Bible book three-letter-abbreviation
        public int CompareTo(bookMatch otherBook)
        {
            if (otherBook == null)
                return -1;
            if (bookName.Length > otherBook.bookName.Length)
                return -1;
            if (bookName.Length < otherBook.bookName.Length)
                return 1;
            return bookName.CompareTo(otherBook.bookName);
        }
    }
}
