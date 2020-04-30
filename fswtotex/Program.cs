/*
    Regarding license, this code (and I say nothing about anything outside of this code)
    is licensed by "The Unlicense".

    You may use for: private use, commercial use, modifiy, and distribute.

    You may not expect: liability, nor warranty.

    This is the same license as the source material:
        https://github.com/dazitzel/SignWritingLaTeX
*/
using System;
using System.IO;

/*
    This parser works with as state machines within state machines.

    There is also a general error state which says that we failed in our matching and need
    to spit out what we have saved so far.

    Our outer state machine has the following states:
        Start       -- the obligatory starting state
        Prefix      -- processing the temporal prefix (can be missing)
        Visual      -- processing the visual layout of the sign (required)
        Punctuation -- processing a punctuation sign
        End         -- Time to spit out what we have

    The prefix state machine has the following states:
        Start       -- the obligatory starting state
        Symbol      -- processing a base symbol
        End         -- If we used this state machine, time for the outer state machine to move to the next state

    The visual state machine has the following states:
        Start       -- the obligatory starting state
        Size        -- processing the size of the word
        Symbol      -- processing a base symbol
        Placement   -- processing the placement of the symbol
        End         -- Time for the outer state machine to finish

    The Punctuation state machine hase the following states:
        Symbol      -- processing a base symbol
        Placement   -- processing the placement of the symbol
        End         -- Time for the outer state machine to finish

    The Symbol state machine has the following states
        Start       -- the obligatory starting state
        FirstDigit  -- the first digit
        SecondDigit -- the second digit
        ThirdDigt   -- the third digit
        Fill        -- the fill
        Rotation    -- the rotation
        End         -- Time for the outer state machine to finish

    The Size state machine has the following states:
        Start       -- the obligatory starting state
        FirstW      -- the first digit of the width
        SecondW     -- the second digit of the width
        ThirdW      -- the third digit of the width
        x           -- the letter `x'
        FirstH      -- the first digit of the height
        SecondH     -- the second digit of the height
        ThirdH      -- the third digit of the height
        End         -- Time for the outer state machine to finish

    The Placement state machine is exactly the same as the size state machine

    When this parsing is complete, it spits what may look like rather odd
    versions of the signs. This is a version specifically adjusted to work
    with the haiola program and ebible.org in general based on an earlier
    version done by me in C++.
*/

namespace fswToTex {
  class program {
    const int s_error=-1;
    const int s_start=0;
    const int s_prefix=1;
    const int s_visual=2;
    const int s_punctuation=3;
    const int s_size=1;
    const int s_symbol=2;
    const int s_placement=3;
    const int s_first=1;
    const int s_second=2;
    const int s_third=3;
    const int s_fill=4;
    const int s_rotation=5;
    const int s_firstw=1;
    const int s_secondw=2;
    const int s_thirdw=3;
    const int s_x=4;
    const int s_firsth=5;
    const int s_secondh=6;
    const int s_thirdh=7;
    const int s_end=8;

    int state      =s_start;
    int substate   =s_start;
    int subsubstate=s_start;

    /*
    Now we come to reading.

    If we want to be able to read from a wide variety of files, then we
    have to be ready for all the different encodings and be able to convert
    each of them into utf-32.

    We will begin here with a set of functions to read a character of a
    given type. Unknown or undetermined as of yet and utf8/=16/32/le/be. We
    will then have a set of functions to convert to utf32(internal version)
    from all the other types of characters.

    Even though these are technically characters, because we are dealing
    with SignWriting characters which are outside of plane-0 we are going
    to pass them around as if they are unsigned integers anyway.
    */

    enum theTextFormat
    {
      unknown, utf8, utf16le, utf16be, utf32le, utf32be
    };
    theTextFormat textFormat = theTextFormat.unknown;

    byte[] charBuff = {0, 0, 0, 0};
    int charBuffSize = 0;
    uint getCharUnknown(FileStream fileIn)
    {
      uint result=0;
      // feff is the byte order
      // In each format, this can serve as a key
      // utf  8    ef bb bf
      // utf 16 le ff fe
      // utf 16 be fe ff
      // utf 32 le ff fe 00 00
      // utf 32 be 00 00 fe ff
      // So if we are unknown then we look for those,
      // and if we don't see any of them we default to utf8
      if(charBuffSize==0)
      {
        for(;charBuffSize<4;charBuffSize++)
        {
          charBuff[charBuffSize]=(byte)fileIn.ReadByte();
        }
        if(charBuff[0]==0x0  && charBuff[1]==0x0 &&
           charBuff[2]==0xfe && charBuff[3]==0xff)
        {
          textFormat=theTextFormat.utf32be;
          return getCharUtf32be(fileIn);
        }
        else if(charBuff[0]==0xff && charBuff[1]==0xfe &&
                charBuff[2]==0x0  && charBuff[3]==0x0)
        {
          textFormat=theTextFormat.utf32le;
          return getCharUtf32le(fileIn);
        }
        else if(charBuff[0]==0xfe && charBuff[1]==0xff)
        {
          textFormat=theTextFormat.utf16be;
          charBuff[0]=charBuff[2];
          charBuff[1]=charBuff[3];
          charBuff[2]=0;
          charBuff[3]=0;
          if(utf16beToUtf32(charBuff,ref result)==2)
          {
            return result;
          }
          charBuff[2]=(byte)fileIn.ReadByte();
          charBuff[3]=(byte)fileIn.ReadByte();
          if(utf16beToUtf32(charBuff,ref result)==4)
          {
            return result;
          }
          throw new System.Exception("Badly formed utf16be string.");
        }
        else if(charBuff[0]==0xff && charBuff[1]==0xfe)
        {
          textFormat=theTextFormat.utf16le;
          charBuff[0]=charBuff[2];
          charBuff[1]=charBuff[3];
          charBuff[2]=0;
          charBuff[3]=0;
          if(utf16leToUtf32(charBuff,ref result)==2)
          {
            return result;
          }
          charBuff[2]=(byte)fileIn.ReadByte();
          charBuff[3]=(byte)fileIn.ReadByte();
          if(utf16leToUtf32(charBuff,ref result)==4)
          {
            return result;
          }
          throw new System.Exception("Badly formed ut16le string.");
        }
        else if(charBuff[0]==0xef && charBuff[1]==0xbb &&
                charBuff[2]==0xbf)
        {
          textFormat=theTextFormat.utf8;
          charBuff[0]=charBuff[3];
          charBuff[1]=0;
          charBuff[2]=0;
          charBuff[3]=0;
          if(utf8ToUtf32(charBuff,ref result)==1)
          {
            return result;
          }
          charBuff[1]=(byte)fileIn.ReadByte();
          if(utf8ToUtf32(charBuff,ref result)==2)
          {
            return result;
          }
          charBuff[2]=(byte)fileIn.ReadByte();
          if(utf8ToUtf32(charBuff,ref result)==3)
          {
            return result;
          }
          charBuff[3]=(byte)fileIn.ReadByte();
          if(utf8ToUtf32(charBuff,ref result)==4)
          {
            return result;
          }
          throw new System.Exception("Badly formed utf8 string.");
        }
      }
      int offset = utf8ToUtf32(charBuff,ref result);
      while(offset<0 && charBuffSize<4)
      {
        charBuff[charBuffSize]=(byte)fileIn.ReadByte();
        charBuffSize++;
        offset = utf8ToUtf32(charBuff,ref result);
      }
      int i;
      for(i=0;i<charBuffSize-offset;i++)
      {
        charBuff[i]=charBuff[i+offset];
      }
      for(;i<charBuffSize;i++)
      {
        charBuff[i]=0;
      }
      charBuffSize-=offset;
      if(charBuffSize==0)
      {
        textFormat=theTextFormat.utf8;
      }
      return result;
    }

    uint getCharUtf8(FileStream fileIn)
    {
      byte[] charBuff={0,0,0,0};
      int read=0;
      int size=0;
      uint result=0;
      do
      {
        read = fileIn.ReadByte();
        if(read==-1)
          return 0xffffffff;
        charBuff[size++]=(byte)read;
      }
      while((size<4) && (utf8ToUtf32(charBuff,ref result)!=size));
      if(utf8ToUtf32(charBuff,ref result)==size)
      {
        return result;
      }
      throw new System.Exception("Badly formed utf8 string.");
    }

    uint getCharUtf16le(FileStream fileIn)
    {
      byte[] charBuff={0,0,0,0};
      int read=0;
      int size=0;
      uint result=0;
      do
      {
        read = fileIn.ReadByte();
        if(read==-1)
          return 0xffffffff;
        charBuff[size++]=(byte)read;
        read = fileIn.ReadByte();
        if(read==-1)
          return 0xffffffff;
        charBuff[size++]=(byte)read;
      }
      while((size<4) && (utf16leToUtf32(charBuff,ref result)!=size));
      if(utf16leToUtf32(charBuff,ref result)==size)
      {
        return result;
      }
      throw new System.Exception("Badly formed utf16le string.");
    }

    uint getCharUtf16be(FileStream fileIn)
    {
      byte[] charBuff={0,0,0,0};
      int read=0;
      int size=0;
      uint result=0;
      do
      {
        read = fileIn.ReadByte();
        if(read==-1)
          return 0xffffffff;
        charBuff[size++]=(byte)read;
        read = fileIn.ReadByte();
        if(read==-1)
          return 0xffffffff;
        charBuff[size++]=(byte)read;
      }
      while((size<4) && (utf16beToUtf32(charBuff,ref result)!=size));
      if(utf16beToUtf32(charBuff,ref result)==size)
      {
        return result;
      }
      throw new System.Exception("Badly formed utf16be string.");
    }

    uint getCharUtf32le(FileStream fileIn)
    {
      byte[] charBuff={0,0,0,0};
      int read=0;
      int size=0;
      uint result=0;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      utf32leToUtf32(charBuff,ref result);
      return result;
    }

    uint getCharUtf32be(FileStream fileIn)
    {
      byte[] charBuff={0,0,0,0};
      int read=0;
      int size=0;
      uint result=0;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      read = fileIn.ReadByte();
      if(read==-1)
        return 0xffffffff;
      charBuff[size++]=(byte)read;
      utf32beToUtf32(charBuff,ref result);
      return result;
    }

    uint getChar(FileStream fileIn)
    {
      switch(textFormat)
      {
        case theTextFormat.utf8   : return getCharUtf8   (fileIn);
        case theTextFormat.utf16le: return getCharUtf16le(fileIn);
        case theTextFormat.utf16be: return getCharUtf16be(fileIn);
        case theTextFormat.utf32le: return getCharUtf32le(fileIn);
        case theTextFormat.utf32be: return getCharUtf32be(fileIn);
        default     : return getCharUnknown(fileIn);
      }
    }

    /*
    We always convert to utf-8 on output since we know that we are
    outputting a lot of XeLaTeX code with expanded characters of the
    format ``\char#xxxx''.
    */

    byte[] utf32ToUtf8(uint c)
    {
      byte[] to = {};
      if(c<0x80)
      {
        Array.Resize(ref to, to.Length+1);
        to[to.Length-1] = (byte)c;
      }
      else if(c<0x800)
      {
        Array.Resize(ref to, to.Length+2);
        to[to.Length-2] = (byte)(((c>>6)&0x1f)|0xc0);
        to[to.Length-1] = (byte)(((c>>0)&0x3f)|0x80);
      }
      else if(c<0x10000)
      {
        Array.Resize(ref to, to.Length+3);
        to[to.Length-3] = (byte)(((c>>12)&0xf)|0xe0);
        to[to.Length-2] = (byte)(((c>>6)&0x3f)|0x80);
        to[to.Length-1] = (byte)(((c>>0)&0x3f)|0x80);
      }
      else
      {
        Array.Resize(ref to, to.Length+3);
        to[to.Length-3] = (byte)(((c>>18)&0x7)|0xf0);
        to[to.Length-3] = (byte)(((c>>12)&0x3f)|0x80);
        to[to.Length-3] = (byte)(((c>>6)&0x3f)|0x80);
        to[to.Length-3] = (byte)(((c>>0)&0x3f)|0x80);
      }
      return to;
    }

    // And now we send it out
    void sendOut(FileStream fileOut, uint c)
    {
      byte[] buffer = utf32ToUtf8(c);
      foreach(byte b in buffer)
        fileOut.WriteByte(b);
    }

    void sendOut(FileStream fileOut, ref uint[] l, uint c)
    {
      int i;
      for(i=0;i<l.Length;i++)
        sendOut(fileOut,l[i]);
      sendOut(fileOut,c);
      Array.Resize(ref l, 0);
      state=substate=subsubstate=s_start;
    }

    // Now that we have been using all those ``convert to uft32'', let's define them.

    int utf8ToUtf32(byte[] coming, ref uint going)
    {
      // Quick review:
      //    0xxx xxxx
      //    110x xxxx  10xx xxxx
      //    1110 xxxx  10xx xxxx  10xx xxxx
      //    1111 -xxx  10xx xxxx  10xx xxxx  10xx xxxx
      if((coming[0]&0xf0)==0xf0)
      {
        going=(uint)coming[0]&0x7;
        for(int i=1;i<4;i++)
        {
          if((coming[i]&0xc0)!=0x80)
            return 0;
          going<<=6;
          going|=(uint)(coming[i]&0x3f);
        }
        return 4;
      }
      if((coming[0]&0xf0)==0xe0)
      {
        going=(uint)coming[0]&0xf;
        for(int i=1;i<3;i++)
        {
          if((coming[i]&0xc0)!=0x80)
            return 0;
          going<<=6;
          going|=(uint)(coming[i]&0x3f);
        }
        return 3;
      }
      if((coming[0]&0xe0)==0xc0)
      {
        going=(uint)coming[0]&0x1f;
        for(int i=1;i<2;i++)
        {
          if((coming[i]&0xc0)!=0x80)
            return 0;
          going<<=6;
          going|=(uint)(coming[i]&0x3f);
        }
        return 2;
      }
      if((coming[0]&0xc0)==0x80)
      {
        throw new System.Exception("Malformed utf8 string.");
      }
      going=coming[0];
      return 1;
    }

    int utf16leToUtf32(byte[] coming, ref uint going)
    {
      // Quick review, after accounting for endianess
      // 0000--d7ff = pass along
      // d800--d8ff = a following dc00-dfff tells the low 10 bits
      // dc00--dfff = better be following d800-d8ff
      // e000--ffff = pass along
      going=(uint)coming[0]<<0;
      going|=(uint)coming[1]<<8;
      if(going<0xd800)
        return 2;
      if(going<0xdc00)
      {
        if(coming[3]<0xdc || coming[3]>0xdf)
          return 0;
        going&=0x3ff;
        going|=0x400;
        going<<=10;
        going|=coming[2];
        going|=(uint)(coming[3]&0x3)<<8;
        return 2;
      }
      if(going<0xe000)
        throw new System.Exception("Malformed utf16le string");
      return 2;
    }

    int utf16beToUtf32(byte[] coming, ref uint going)
    {
      // Quick review, after accounting for endianess
      // 0000--d7ff = pass along
      // d800--d8ff = a following dc00-dfff tells the low 10 bits
      // dc00--dfff = better be following d800-d8ff
      going=(uint)coming[0]<<8;
      going|=(uint)coming[1]<<0;
      if(going<0xd800)
        return 2;
      if(going<0xdc00)
      {
        if(coming[2]<0xdc || coming[2]>0xdf)
          return 0;
        going&=0x3ff;
        going|=0x400;
        going<<=10;
        going|=(uint)(coming[2]&0x3)<<8;
        going|=(uint)coming[3];
        return 2;
      }
      if(going<0xe000)
        throw new System.Exception("Malformed utf16le string");
      return 2;
    }

    int utf32leToUtf32(byte[] coming, ref uint going)
    {
      going=(uint)coming[0]<<0;
      going|=(uint)coming[1]<<8;
      going|=(uint)coming[2]<<16;
      going|=(uint)coming[3]<<24;
      return 4;
    }

    int utf32beToUtf32(byte[] coming, ref uint going)
    {
      going=(uint)coming[0]<<24;
      going|=(uint)coming[1]<<16;
      going|=(uint)coming[2]<<8;
      going|=(uint)coming[3]<<0;
      return 4;
    }

    // A couple more helper functions to keep the code clear
    void Add(ref uint[] line, uint toAdd)
    {
        Array.Resize(ref line, line.Length+1);
        line[line.Length-1] = toAdd;
    }

    void stringToByte(string coming, ref byte[] going)
    {
      uint tempuint = 0;
      byte[] tempgoing = {};
      Array.Resize(ref going, 0);
      for(int i=0;i<coming.Length;i++)
      {
        tempuint=(uint)coming[i];
        tempgoing = utf32ToUtf8(tempuint);
        Array.Resize(ref going, going.Length+tempgoing.Length);
        for(int j=0; j < tempgoing.Length; j++)
          going[going.Length-tempgoing.Length+j] = tempgoing[j];
      }
    }

    // Finally, let's explain and start the program

    int usage()
    {
      Console.WriteLine("This is fswtotex.");
      Console.WriteLine("");
      Console.WriteLine("This program expects exactly two arguments:");
      Console.WriteLine("    One input file containing Formal SignWriting embedded in LaTex;");
      Console.WriteLine("    One output file which will contain TiKz embedded in LaTeX.");
      Console.WriteLine("");
      return -1;
    }

    static int Main(string[] args)
    {
      int result=0;
      //  When we run we expect exactly two arguments:
      //      the file to read from and;
      //      The file to write to.
      //  Once we have that, we let it rip.

      try
      {
        program me = new program();
        if(args.Length!=2)
        {
          return me.usage();
        }
        if(args[0][0]=='-' || args[1][0]=='-')
        {
          return me.usage();
        }
        FileStream fileIn = new FileStream(args[0], FileMode.Open, FileAccess.Read);
        FileStream fileOut= new FileStream(args[1], FileMode.Create, FileAccess.Write);
        result = me.fswtotex(fileIn, fileOut);
        string line="";
        byte[] toSend={};
        line = "% This file was generated by:\n";
        me.stringToByte(line, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        line = "%    fswtotex " + args[0] + " " + args[1] + "\n";
        me.stringToByte(line, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        fileOut.Close();
        fileIn.Close();
      }
      catch (System.Exception ex)
      {
        Console.WriteLine("Failure: "+ex.Message+" stack:\n"+ex.StackTrace);
        result = -1;
      }
      return result;
    }

    /*
    Now we have our state processing functions.
    For each set of states we have a separate function that expects a uint
    (which represents the Unicode character in question). There are three possible
    results from being in a state and receiving a character.
    1) Move forward to a new state.
    2) Notice an error and spit out the currently stored data unchanged
    3) Notice that the match is complete and translate it.

    By far the largest section of code will be to move forward to a new state,
    and each potential move forward will hake a check for errors. Occasionally,
    an ``error'' state will actually indicate a successful completion and we
    will do a translation.

    Let's start with our storage and state management followed by declaring our
    state progression functions.

    One final note, this SignWriting converter is actually too permissive.
    In this converter you can use unicode for the symbol and ``text'' for
    the numbers, or the reverse. I believe that you should actually be
    required to do one or the other, but there it is. If I were ever to make a
    version that did not let you mix and match FSWA and FSWU, we would have two
    sets of functions.

    It's may also not be permissive enough in that if you have "AS123M", it may
    miss that M starts a word. I didn't bother testing for this behavior. I also
    don't look through a partially accepted string to see if a new one should
    start so if you say "M500x500S10000500x500S" then this is a failure and the
    first portion will not be translated.

    Each of these states tells us what is being expected. So, for instance,
    start_start_start is expecting to see a word start. If it doesn't, then it just
    sends the character along. But visual_size_secondw is expecting the second digit
    of the number expressing the width. If it doesn't, then it will need to spit out
    what it has already stored and then go back to start. This is also why there
    aren't any functions for "_end", that would mean that it's expecting one
    character past the last one.
    */

    uint[] line = {};

    int fswtotex(FileStream fileIn, FileStream fileOut)
    {
      state=substate=subsubstate=s_start;
      uint c=0;
      while(c!=0xffffffff)
      {
        c=getChar(fileIn);
        if(c==0xffffffff)
          continue;
        if     (state==s_start)       start (fileOut, c);
        else if(state==s_prefix)      prefix(fileOut, c);
        else if(state==s_visual)      visual(fileOut, c);
        else if(state==s_punctuation) punctuation(fileOut, c);
        else throw new System.Exception("Unknown state.");
      }
      string line="";
      byte[] toSend={};
      line = "\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% In order for this conversion to work your document needs a few things.\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% \\usepackage{fontspec}\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% \\usepackage{tikz}\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% \\begin{document}\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% \\newfontfamily\\swfill{SuttonSignWritingFill.ttf}\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      line="% \\newfontfamily\\swline{SuttonSignWritingLine.ttf}\n";
      stringToByte(line, ref toSend);
      fileOut.Write(toSend,0,toSend.Length);
      return 0;
    }

    /*
    We are going to cover these states breadth first.
    That is, all the base states, then the two-level states, then ...
    */

    void start(FileStream fileOut, uint c)
    {
      if(substate==s_start) start_start(fileOut, c);
      else throw new System.Exception("Unknown substate in start.");
    }

    void prefix(FileStream fileOut, uint c)
    {
      if(substate==s_symbol) prefix_symbol(fileOut, c);
      else throw new System.Exception("Unknown substate in prefix.");
    }

    void visual(FileStream fileOut, uint c)
    {
      if     (substate==s_start)     visual_start    (fileOut, c);
      else if(substate==s_size)      visual_size     (fileOut, c);
      else if(substate==s_symbol)    visual_symbol   (fileOut, c);
      else if(substate==s_placement) visual_placement(fileOut, c);
      else throw new System.Exception("Unknown substate in visual.");
    }

    void punctuation(FileStream fileOut, uint c)
    {
      if     (substate==s_start)     punctuation_start    (fileOut, c);
      else if(substate==s_symbol)    punctuation_symbol   (fileOut, c);
      else if(substate==s_placement) punctuation_placement(fileOut, c);
      else throw new System.Exception("Unknown substate in punctuation.");
    }

    void start_start(FileStream fileOut, uint c)
    {
      if(subsubstate==s_start) start_start_start(fileOut, c);
      else throw new System.Exception("Unknown subsubstate start, start.");
    }

    void prefix_symbol(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_start)    prefix_symbol_start   (fileOut, c);
      else if(subsubstate==s_first)    prefix_symbol_first   (fileOut, c);
      else if(subsubstate==s_second)   prefix_symbol_second  (fileOut, c);
      else if(subsubstate==s_third)    prefix_symbol_third   (fileOut, c);
      else if(subsubstate==s_fill)     prefix_symbol_fill    (fileOut, c);
      else if(subsubstate==s_rotation) prefix_symbol_rotation(fileOut, c);
      else throw new System.Exception("Unknown subsubstate in prefxi, symbol.");
    }

    void visual_start(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_start) visual_start_start(fileOut, c);
      else throw new System.Exception("Unknown subsubstate in visual, start.");
    }

    void visual_size(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_firstw)  visual_size_firstw (fileOut, c);
      else if(subsubstate==s_secondw) visual_size_secondw(fileOut, c);
      else if(subsubstate==s_thirdw)  visual_size_thirdw (fileOut, c);
      else if(subsubstate==s_x)       visual_size_x      (fileOut, c);
      else if(subsubstate==s_firsth)  visual_size_firsth (fileOut, c);
      else if(subsubstate==s_secondh) visual_size_secondh(fileOut, c);
      else if(subsubstate==s_thirdh)  visual_size_thirdh (fileOut, c);
      else throw new System.Exception("Unknown subsubstate in visual, size.");
    }

    void visual_symbol(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_start)    visual_symbol_start   (fileOut, c);
      else if(subsubstate==s_first)    visual_symbol_first   (fileOut, c);
      else if(subsubstate==s_second)   visual_symbol_second  (fileOut, c);
      else if(subsubstate==s_third)    visual_symbol_third   (fileOut, c);
      else if(subsubstate==s_fill)     visual_symbol_fill    (fileOut, c);
      else if(subsubstate==s_rotation) visual_symbol_rotation(fileOut, c);
      else throw new System.Exception("Unknown subsubstate int visual, symbol.");
    }

    void visual_placement(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_firstw)  visual_placement_firstw (fileOut, c);
      else if(subsubstate==s_secondw) visual_placement_secondw(fileOut, c);
      else if(subsubstate==s_thirdw)  visual_placement_thirdw (fileOut, c);
      else if(subsubstate==s_x)       visual_placement_x      (fileOut, c);
      else if(subsubstate==s_firsth)  visual_placement_firsth (fileOut, c);
      else if(subsubstate==s_secondh) visual_placement_secondh(fileOut, c);
      else if(subsubstate==s_thirdh)  visual_placement_thirdh (fileOut, c);
      else if(subsubstate==s_end)     visual_placement_end    (fileOut, c);
      else throw new System.Exception("Unknown subsubstate in visual, placement.");
    }

    void punctuation_start(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_start) punctuation_start_start(fileOut, c);
      else throw new System.Exception("Unknown subsubstate in punctuation, start.");
    }

    void punctuation_symbol(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_start)    punctuation_symbol_start   (fileOut, c);
      else if(subsubstate==s_first)    punctuation_symbol_first   (fileOut, c);
      else if(subsubstate==s_second)   punctuation_symbol_second  (fileOut, c);
      else if(subsubstate==s_third)    punctuation_symbol_third   (fileOut, c);
      else if(subsubstate==s_fill)     punctuation_symbol_fill    (fileOut, c);
      else if(subsubstate==s_rotation) punctuation_symbol_rotation(fileOut, c);
      else throw new System.Exception("Unknown subsubstate int punctuation, symbol.");
    }

    void punctuation_placement(FileStream fileOut, uint c)
    {
      if     (subsubstate==s_firstw)  punctuation_placement_firstw (fileOut, c);
      else if(subsubstate==s_secondw) punctuation_placement_secondw(fileOut, c);
      else if(subsubstate==s_thirdw)  punctuation_placement_thirdw (fileOut, c);
      else if(subsubstate==s_x)       punctuation_placement_x      (fileOut, c);
      else if(subsubstate==s_firsth)  punctuation_placement_firsth (fileOut, c);
      else if(subsubstate==s_secondh) punctuation_placement_secondh(fileOut, c);
      else if(subsubstate==s_thirdh)  punctuation_placement_thirdh (fileOut, c);
      else if(subsubstate==s_end)     punctuation_placement_end    (fileOut, c);
      else throw new System.Exception("Unknown subsubstate in punctuation, placement.");
    }

    void start_start_start(FileStream fileOut, uint c)
    {
      if(c=='A' || c==0x1d800)
        { Add(ref line,c); state=s_prefix; substate=s_symbol; subsubstate=s_start; }
      else if(c=='B' || (c>='L'&&c<='M') || c=='R' || (c>=0x1d801&&c<=0x1d804))
        { Add(ref line,c); state=s_visual; substate=s_size; subsubstate=s_firstw; }
      else if(c=='S')
        { Add(ref line,c); state=s_punctuation; substate=s_symbol; subsubstate=s_first; }
      else
        sendOut(fileOut, c);
    }

    void prefix_symbol_start(FileStream fileOut, uint c)
    {
       if(c=='S')
        { Add(ref line, c); subsubstate=s_first; }
       else if(c>=0x40001 && c<=0x4f428)
        { Add(ref line, c); state=s_visual; substate=s_start; subsubstate=s_start; }
      else
        sendOut(fileOut, ref line, c);
    }

    void prefix_symbol_first(FileStream fileOut, uint c)
    {
      if(c>='1' && c<='3')
        { Add(ref line, c); subsubstate=s_second; }
      else
        sendOut(fileOut, ref line, c);
    }

    void prefix_symbol_second(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]>='0'&&line[line.Length-1]<='2')
      {
        if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
          { Add(ref line, c); subsubstate=s_third; }
        else
          sendOut(fileOut, ref line, c);
      }
      else
      {
        if(c>='0' && c<='8')
          { Add(ref line, c); subsubstate=s_third; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void prefix_symbol_third(FileStream fileOut, uint c)
    {
      if(line[line.Length-2]>='0'&&line[line.Length-2]<='2')
      {
        if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
          { Add(ref line, c); subsubstate=s_fill; }
        else
            sendOut(fileOut, ref line, c);
      }
      else
      {
        if(line[line.Length-1]>='0'&&line[line.Length-1]<='7')
        {
          if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
            { Add(ref line, c); subsubstate=s_fill; }
          else
            sendOut(fileOut, ref line, c);
        }
        else
        {
          if((c>='0'&&c<='9') || (c>='a'&&c<='b'))
            { Add(ref line, c); subsubstate=s_fill; }
          else
            sendOut(fileOut, ref line, c);
        }
      }
    }

    void prefix_symbol_fill(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='5')
        { Add(ref line, c); subsubstate=s_rotation; }
      else
        sendOut(fileOut, ref line, c);
    }

    void prefix_symbol_rotation(FileStream fileOut, uint c)
    {
      if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
        { Add(ref line, c); state=s_visual; substate=subsubstate=s_start; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_start_start(FileStream fileOut, uint c)
    {
      if(c=='B' || (c>='L'&&c<='M') || c=='R' || (c>=0x1d801&&c<=0x1d804))
        { Add(ref line, c); substate=s_size; subsubstate=s_firstw; }
      else if(c=='S')
        { Add(ref line, c); state=s_prefix; substate=s_symbol; subsubstate=s_first; }
      else if(c>=0x40001 && c<=0x4f428)
        { Add(ref line, c); state=s_visual; substate=s_start; subsubstate=s_start; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_size_firstw(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondw; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_size_secondw(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void visual_size_thirdw(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); subsubstate=s_x; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_size_x(FileStream fileOut, uint c)
    {
      if(c=='x')
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_size_firsth(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondh; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); substate=s_symbol; subsubstate=s_start; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_size_secondh(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void visual_size_thirdh(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); substate=s_symbol; subsubstate=s_start; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_symbol_start(FileStream fileOut, uint c)
    {
      if(c=='S')
        { Add(ref line, c); subsubstate=s_first; }
      else if(c>=0x40001 && c<=0x4f428)
        { Add(ref line, c); state=s_visual; substate=s_placement; subsubstate=s_first; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_symbol_first(FileStream fileOut, uint c)
    {
      if(c>='1' && c<='3')
        { Add(ref line, c); subsubstate=s_second; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_symbol_second(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]>='0'&&line[line.Length-1]<='2')
      {
        if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
          { Add(ref line, c); subsubstate=s_third; }
        else
          sendOut(fileOut, ref line, c);
      }
      else
      {
        if(c>='0' && c<='8')
          { Add(ref line, c); subsubstate=s_third; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void visual_symbol_third(FileStream fileOut, uint c)
    {
      if(line[line.Length-2]>='0'&&line[line.Length-2]<='2')
      {
        if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
          { Add(ref line, c); subsubstate=s_fill; }
        else
          sendOut(fileOut, ref line, c);
      }
      else
      {
        if(line[line.Length-1]>='0'&&line[line.Length-1]<='7')
        {
          if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
            { Add(ref line, c); subsubstate=s_fill; }
          else
            sendOut(fileOut, ref line, c);
        }
        else
        {
          if((c>='0'&&c<='9') || (c>='a'&&c<='b'))
            { Add(ref line, c); subsubstate=s_fill; }
          else
            sendOut(fileOut, ref line, c);
        }
      }
    }

    void visual_symbol_fill(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='5')
        { Add(ref line, c); subsubstate=s_rotation; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_symbol_rotation(FileStream fileOut, uint c)
    {
      if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
        { Add(ref line, c); substate=s_placement; subsubstate=s_firstw; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_firstw(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondw; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_secondw(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void visual_placement_thirdw(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); subsubstate=s_x; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_x(FileStream fileOut, uint c)
    {
      if(c=='x')
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_firsth(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondh; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); subsubstate=s_end; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_secondh(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void visual_placement_thirdh(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); subsubstate=s_end; }
      else
        sendOut(fileOut, ref line, c);
    }

    void visual_placement_end(FileStream fileOut, uint c)
    {
      if(c=='S')
        { Add(ref line, c); substate=s_symbol; subsubstate=s_first; }
      else if(c>=0x40001 && c<=0x4f428)
        { Add(ref line, c); state=s_visual; substate=s_placement; subsubstate=s_first; }
      else
      {
        state=substate=subsubstate=s_start;
        uint place=0;
        char lane='B';
        if(line[place]=='A' || line[place]==0x1d800)
        {
           place++;
           while(line[place]=='S' || (line[place]>=0x40001 && line[place]<=0x4f428))
           {
             if(line[place]=='S') place+=6;
             else                 place++;
           }
        }
        if(line[place]=='B' || line[place]==0x1d801)
          lane='B';
        if(line[place]=='L' || line[place]==0x1d802)
          lane='L';
        if(line[place]=='M' || line[place]==0x1d803)
          lane='M';
        if(line[place]=='R' || line[place]==0x1d804)
          lane='R';
        place++;
        // We currently ignore the height and width, so we just skip over them.
        if(line[place]>'2' && line[place]<='7')
        {
          place += 3;
          // and skip the x
          place++;
        }
        else
        {
          place++;
        }
        if(line[place]>'2' && line[place]<='7')
        {
          place += 3;
        }
        else
        {
          place++;
        }
        string lineOut="";
        byte[] toSend={};
        lineOut="{\\uccoff";
        stringToByte(lineOut, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        lineOut="\\begin{tikzpicture}[rotate=-90,yscale=-1]";
        stringToByte(lineOut, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        // The idea was, initally, to place a thin rectangle behind each character from 0--1000.
        // Unfortunately, this made it so that I could reasonably fit about two columns of
        // \normalsize text to a page. We are now only extend 100 each direction to allow for about
        // five columns of \normalsize text. This also decided our lane shift amount later on.

        // Yes, I know the number 100 doesn't appear. That's because I found through experimentation
        // that a character anchored at (0,0) along with a rectangle around (0,0) does not place a
        // rectangle around the expected corner.
        if(lane != 'B')
        {
          lineOut="\\draw[white](\\FontSize/30*-90 pt,\\FontSize/30*-12 pt)rectangle(\\FontSize/30*110 pt,\\FontSize/30*-10 pt);";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
        }
        while(place<line.Length)
        {
          int s;
          if(line[place]=='S')
          {
            place++;
            s=(int)(line[place]-'0')*256;
            if(line[place+1]>='0' && line[place+1]<='9')
              s+=(int)(line[place+1]-'0')*16;
            else // if(line[place+1]>='a' && line[place+1]<='f')
              s+=(int)(line[place+1]-'a'+10)*16;
            if(line[place+2]>='0' && line[place+2]<='9')
              s+=(int)(line[place+2]-'0')*1;
            else // if(line[place+2]>='a' && line[place+2]<='f')
              s+=(int)(line[place+2]-'a'+10)*1;
            place+=3;
            s-=0x100;
            s*=(6*16);
            s+=(int)(line[place]-'0')*16;
            place++;
            if(line[place]>='0' && line[place]<='9')
              s+=(int)(line[place]-'0');
            else // if(line[place+1]>='a' && line[place+1]<='f')
              s+=(int)(line[place]-'a'+10);
            place++;
          }
          else // if(line[place]>=0x40001 && line[place]<=0x4f428)
          {
            s=(int)line[place]-0x40001;
            place++;
          }
          int sx;
          if(line[place]>'2' && line[place]<='7')
          {
            sx=(int)((line[place]-'0')*100 + (line[place+1]-'0')*10 + (line[place+2]-'0'));
            place += 3;
            // and skip the x
            place++;
          }
          else
          {
            sx=(int)line[place]-0x1d80c+250;
            place++;
          }
          int sy;
          if(line[place]>'2' && line[place]<='7')
          {
            sy=(int)((line[place]-'0')*100 + (line[place+1]-'0')*10 + (line[place+2]-'0'));
            place += 3;
          }
          else
          {
            sy=(int)line[place]-0x1d80c+250;
            place++;
          }
          /*
          At this point, assuming well formed F/USW strings, we will
          Have a symbol centered around (500,500).
          For 'B' (meaning horizontal SW) we center it around (0,0).
          For 'L' we shift it left 250.
          For 'M' it is correct.
          For 'R' we shift it right 250.
          */
          if(lane=='B')
            sx-=500;
          if(lane=='L')
            sx-=550;
          if(lane=='M')
            sx-=500;
          if(lane=='R')
            sx-=450;
          sy-=500;
          // Now we know where, but for SignWriting the white space can be important too.
          lineOut="\\draw(\\FontSize/30*";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=sx.ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=" pt,\\FontSize/30*";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=(-sy).ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=" pt) node [xscale=-1,rotate=-90,color=white,anchor=north west] {\\swfill\\fontsize{\\FontSize}{\\FontSize}\\selectfont\\char";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=(0x100001+s).ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut="};";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut="\\draw(\\FontSize/30*";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=sx.ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=" pt,\\FontSize/30*";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=(-sy).ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=" pt) node [xscale=-1,rotate=-90,anchor=north west] {\\swline\\fontsize{\\FontSize}{\\FontSize}\\selectfont\\char";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut=(0xf0001+s).ToString();
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
          lineOut="};";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
        }
        lineOut="\\end{tikzpicture}";
        stringToByte(lineOut, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        lineOut="\\uccon}";
        stringToByte(lineOut, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        /*if(lane!='B')
        {
          lineOut="\\\\";
          stringToByte(lineOut, ref toSend);
          fileOut.Write(toSend,0,toSend.Length);
        }*/
        Array.Resize(ref line, 0);
        lineOut=((char)(c)).ToString();
        stringToByte(lineOut, ref toSend);
        fileOut.Write(toSend,0,toSend.Length);
        state=substate=subsubstate=s_start;
      }
    }

    void punctuation_start_start(FileStream fileOut, uint c)
    {
      if(c=='S')
        { Add(ref line, c); substate=s_symbol; subsubstate=s_first; }
      else if(c>=0x4f424 && c<=0x4f428)
        { Add(ref line, c); substate=s_placement; subsubstate=s_first; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_start(FileStream fileOut, uint c)
    {
      if(c=='S')
        { Add(ref line, c); substate=s_symbol; subsubstate=s_first; }
      else if(c>=0x4f424 && c<=0x4f428)
        { Add(ref line, c); substate=s_placement; subsubstate=s_first; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_first(FileStream fileOut, uint c)
    {
      if(c=='3')
        { Add(ref line, c); subsubstate=s_second; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_second(FileStream fileOut, uint c)
    {
      if(c=='8')
        { Add(ref line, c); subsubstate=s_third; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_third(FileStream fileOut, uint c)
    {
      if((c>='7'&&c<='9') || (c>='a'&&c<='b'))
        { Add(ref line, c); subsubstate=s_fill; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_fill(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='5')
        { Add(ref line, c); subsubstate=s_rotation; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_symbol_rotation(FileStream fileOut, uint c)
    {
      if((c>='0'&&c<='9') || (c>='a'&&c<='f'))
        { Add(ref line, c); substate=s_placement; subsubstate=s_firstw; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_firstw(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondw; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_secondw(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdw; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void punctuation_placement_thirdw(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); subsubstate=s_x; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_x(FileStream fileOut, uint c)
    {
      if(c=='x')
        { Add(ref line, c); subsubstate=s_firsth; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_firsth(FileStream fileOut, uint c)
    {
      if(c>='2' && c<='7')
        { Add(ref line, c); subsubstate=s_secondh; }
      else if(c>=0x1d80c && c<=0x1d9ff)
        { Add(ref line, c); subsubstate=s_end; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_secondh(FileStream fileOut, uint c)
    {
      if(line[line.Length-1]=='2')
      {
        if( c>='5' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else if(line[line.Length-1]>='3'&&line[line.Length-1]<='6')
      {
        if( c>='0' && c<='9')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
      else // if(line[line.Length-1]=='7')
      {
        if( c>='0' && c<='4')
          { Add(ref line, c); subsubstate=s_thirdh; }
        else
          sendOut(fileOut, ref line, c);
      }
    }

    void punctuation_placement_thirdh(FileStream fileOut, uint c)
    {
      if(c>='0' && c<='9')
        { Add(ref line, c); subsubstate=s_end; }
      else
        sendOut(fileOut, ref line, c);
    }

    void punctuation_placement_end(FileStream fileOut, uint c)
    {
      uint[] temp = {'M','5','0','0','x','5','0','0'};
      foreach(uint u in line)
          Add(ref temp, u);
      Array.Resize(ref line, 0);
      foreach(uint u in temp)
          Add(ref line, u);
      visual_placement_end(fileOut, c);
    } 
  }
}
