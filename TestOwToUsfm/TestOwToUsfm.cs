using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;

namespace TestOwToUsfm
{
    /// <summary>
    /// This is a set of tests for several files containing regular expressions used to convert the Kupang and Hawaii Pidgin and other OurWord input file sets
    /// to USFM.
    /// 
    /// Currently the RE files in the testRe folder must be copied to C:\testRe to allow the tests to find them.
    /// Unfortunately the old .cct files must be copied to C:
    /// 
    /// The current sequence of steps required to pre-process the Kupang files is
    /// PreProcess.re
    /// footnotes.process
    /// Ow_To_USFM.re
    /// PostProcess.re
    /// 
    /// The OW_To_USFM.re provides very nearly the same conversion as John Duerkson's OW_to_PT.cct as modified for the old Prophero.
    /// The tests verify in most cases that the two conversions do the same thing. In a few cases there are deliberate differences:
    /// - the processing of the various fields involved in figures was too difficult to reproduce in regular expressions,
    /// and since figures are not currently used in any of these translations (online), the new processing simply deletes these fields.
    /// - in a few places there are subtle differences in line breaking
    /// 
    /// PostProcess.re provides roughly the same conversion as the old move_footnote_to_fn.cct combined with cleanup_OW_to_USFM.cct.
    /// In addition it deletes notes and replaces backslashes within \rem fields, which otherwise the next stage of Haiola complains about.
    /// </summary>
    [TestFixture]
    internal class TestOwToUsfm
    {
        [Test]
        public void MultipleNewlines_AreCollapsed()
        {
            string input = @"\id gen
\c 1



\v 1";
            string output = @"\id gen
\c 1
\v 1";
            VerifyConversion(input, output);
        }

        [Test]
        public void IsolatedNewlines_AreChangedToSpaces()
        {
            string input = @"\id gen
\s2 This heading
was on multiple lines
\v 1";
            string output = @"\id gen
\s2 This heading was on multiple lines
\v 1";
            VerifyConversion(input, output);
        }

        [Test]
        public void MaterialBeforeID_IsMovedAfterId()
        {
            string input = @"\s2 try this
\s2 and this
\id gen
\c 1";
            string output = @"\id gen
\s2 try this
\s2 and this
\c 1";
            VerifyConversion(input, output);
        }

        [Test]
        public void Rcrd_IsChangedToRem()
        {
            string input = @"\id gen
\rcrd something
\rcrd
something else
\v 1";
            string output = @"\id gen
\rem RCRD: something
\rem RCRD: something else
\v 1";
            VerifyConversion(input, output);
        }

        [Test]
        public void UnwantedMaterial_IsRemoved()
        {
            string input = @"\id gen
\_ some junk
\e more junk
\z yet more
\c 1";
            string output = @"\id gen
\c 1";
            VerifyConversion(input, output);
        }

        [Test]
        public void AngleBrackets_AreConvertedToQuotes()
        {
            string input = @"\id gen
\c 1
\v 1
\mt <Single quote>
\s2 <<This is a quote>>
\h <<<Nested quote>>>";
            string output = @"\id gen
\c 1
\v 1
\mt ‘Single quote’
\s2 “This is a quote”
\h “‘Nested quote’”";
            VerifyConversion(input, output);
        }

        [Test]
        public void WhiteSpaceAfterMarkersRequiringNewline_IsNormalized()
        {
            string input =
                @"\id gen
\b
text
\b other
\m
text
\m other
\m2
text
\m2 other
\p
text
\p other
\pi
text
\pi other
\q
text
\q other
\q2
text
\q2 other
\q3
text
\q3 other
\qc
text
\qc other
\qm
text
\qm other";
            string output =
                @"\id gen
\b
text
\b
other
\m
text
\m
other
\m2
text
\m2
other
\p
text
\p
other
\pi
text
\pi
other
\q
text
\q
other
\q2
text
\q2
other
\q3
text
\q3
other
\qc
text
\qc
other
\qm
text
\qm
other";
            VerifyConversion(input, output);
        }

        /// <summary>
        /// These are tested explicitly just to be sure, because they were in the old CC table.
        /// There is no special case for them in the RE table, because they are handled by the general rule
        /// of changing newlines to space when not before a marker.
        /// </summary>
        [Test]
        public void WhiteSpaceAfterMarkersRequiringSpace_IsNormalized()
        {
            string input =
                @"\id gen
\c
1
\c 2
\h
head
\h head2
\mt
text
\mt other
\r
text
\r other
\s1
text
\s1 other
\s2
text
\s2 other
\v
text
\v other";
            string output =
                @"\id gen
\c 1
\c 2
\h head
\h head2
\mt text
\mt other
\r text
\r other
\s1 text
\s1 other
\s2 text
\s2 other
\v text
\v other";
            VerifyConversion(input, output);
        }

        [Test]
        public void S_IsNormalizedToS1()
        {
            string input = @"\id gen
\s
text
\s other
\v 1";
            string output = @"\id gen
\s1 text
\s1 other
\v 1";
            ;
            VerifyConversion(input, output);
        }


        [Test]
        public void InternalNoteMarkers_AreConverted()
        {
            string input =
                @"\id gen
\bt text [\s2] more [\q3] and more \va and \ant yet
\bt more\mr still \mr)more \lf special \ov)markers\ov]to covert \c 1
\s2 text [\s2] more [\q3] and more \va and \ant yet \mr still \mr)more \lf special \ov)markers\ov]to covert";
            string output =
                @"\id gen
\note BT: text [s2] more [q3] and more variant and antonym yet\note*
\note BT: moremorphologystill morphology)more lexical function special older version)markersolder version]to covert \note*
\c 1
\s2 text [***\s2] more [***\q3] and more ***\va and ***\ant yet ***\mr still ***\mr)more ***\lf special ***\ov)markers***\ov]to covert";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void BoldAndItalic_AreMarkedBothEnds()
        {
            string input = @"\id gen
\s2 text |bbold|r |iitalic|r |b|ibold italic|r |bbold |ibold italic|r
\v 1";
            string output =
                @"\id gen
\s2 text \bd bold\bd*  \it italic\it*  \bd \it bold italic\it* \bd*  \bd bold \it bold italic\it* \bd* 
\v 1";
            ;
            VerifyConversion(input, output);

            // old CC table does not handle this case correctly.
            input = @"\id gen
\s2 text |iitalic |bbold italic|r
\v 1";
            output = @"\id gen
\s2 text \it italic \bd bold italic\bd* \it* 
\v 1";
            VerifyReConversion(input, output);
        }

        [Test]
        public void Vt_VanishesButPreservesNewline()
        {
            string input = @"\id gen
\vt some text
\vt
more text
\vt yet more
\v 1";
            string output = @"\id gen
some text
more text
yet more
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void St_ChangesToMt2()
        {
            string input = @"\id gen
\st some text
\st
more text
\v 1";
            string output = @"\id gen
\mt2 some text
\mt2 more text
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void CfWithoutColon_ProducesXPlusXoAndXStar()
        {
            string input = @"\id gen
\cf some text
\cf
more text
\v 1";
            string output = @"\id gen
\x + \xo some text
\x*
\x + \xo more text
\x*
\v 1";
            VerifyConversion(input, output);
        }

        // Enhance JohnT: Should colon be converted even if not followed by space?
        // Enhance JohnT: Do we require to handle more than one colon in a single \cf? Current CC table will, RE version won't.
        // Enhance JohnT: these rules produce \xt and \xo as markers not preceded by newline.
        // As a result, putting *** before these markers is disabled everywhere.
        // Ideally it should only be disabled within \x.
        [Test]
        public void ColonInCf_ProducesXt()
        {
            string input = @"\id gen
\cf some: text
\cf split:
text
\v 1";
            string output = @"\id gen
\x + \xo some \xt text
\x*
\x + \xo split \xt text
\x*
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void
            RefInCf_ProducesXt()
        {
            string input = @"\id gen
\cf 1:4: 2 Korintus 8:23, Galatia 2:3, 2 Timotius 4:10
\v 1";
            string output = @"\id gen
\x + \xo 1:4 \xt 2 Korintus 8:23, Galatia 2:3, 2 Timotius 4:10
\x*
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void FtWithoutColon_ProducesFPlusFrandFStar()
        {
            string input = @"\id gen
\ft some text
\ft
more text
\v 1";
            string output = @"\id gen
\f + \fr some text
\f*
\f + \fr more text
\f*
\v 1";
            VerifyConversion(input, output);
        }

        // Enhance JohnT: Should colon be converted even if not followed by space?
        // Enhance JohnT: Do we require to handle more than one colon in a single \cf? Current CC table will, RE version won't.
        // Enhance JohnT: these rules produce \xt and \xo as markers not preceded by newline.
        // As a result, putting *** before these markers is disabled everywhere.
        // Ideally it should only be disabled within \x.
        [Test]
        public void ColonInFt_ProducesFt()
        {
            string input = @"\id gen
\ft some: text
\ft split:
text
\v 1";
            string output = @"\id gen
\f + \fr some \ft text
\f*
\f + \fr split \ft text
\f*
\v 1";
            ;
            VerifyConversion(input, output);
        }

        // This group of tests documents some of the behavior of the old CC table for the four OW tags that document figures:
        // cat, ref, cap, des.
        // The behavior uses stores in a complex way to collect information from potentially repeated or out-of-order
        // elements into a single \fig field, in a way that is very difficult if not impossible to fully reproduce using REs.
        // Since the current web publication strategy does not use figures, I have chosen instead to simply delete these fields.
        // Thus, the tests document different behavior for the two engines.
        [Test]
        public void Cat_ProducesFig()
        {
            string input = @"\id gen
\cat some text
\v 1
\cat
some text
\v 2";
            string outputCc = @"\id gen
\fig |some text
||||| \fig*
\v 1
\fig |some text
||||| \fig*
\v 2";
            VerifyCcConversion(input, outputCc);
            string outputRe = @"\id gen
\v 1
\v 2";
            VerifyReConversion(input, outputRe);
        }

        [Test]
        public void CatRefCapDes_ProducesFig()
        {
            string input = @"\id gen
\cat some text
\ref where from
\cap caption
\des description
\v 1";
            string outputCc = @"\id gen
\fig description |some text
|where from |||caption | \fig*
\v 1";
            VerifyCcConversion(input, outputCc);
            string outputRe = @"\id gen
\v 1";
            VerifyReConversion(input, outputRe);
        }

        /// <summary>
        /// old CC table combines these into a fig description.
        /// Web is not yet using figures due to copyright, so we just get rid of them.
        /// </summary>
        [Test]
        public void DesCatRefCap_ProducesFig()
        {
            string input = @"\id gen
\des description
\cat some text
\ref where from
\cap caption
\v 1";
            string outputCc = @"\id gen
\fig description |some text
|where from |||caption | \fig*
\v 1";
            VerifyCcConversion(input, outputCc);
            string outputRe = @"\id gen
\v 1";
            VerifyReConversion(input, outputRe);
        }

        [Test]
        public void HistProducesRem()
        {
            string input = @"\id gen
\hist some history
\v 1";
            string output = @"\id gen
\rem HIST:  some history
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void VariousMarkersAreTreatedAsNotes()
        {
            MarkerIsTreatedAsNote("al", "Alternate", false);
            MarkerIsTreatedAsNote("ov", "OV", false);
            MarkerIsTreatedAsNote("t", "T", false);
            MarkerIsTreatedAsNote("nt", "NT", true);
            MarkerIsTreatedAsNote("ntgk", "NTGK", true);
            MarkerIsTreatedAsNote("ntck", "NTCK", true);
            MarkerIsTreatedAsNote("chk2", "CHK2", true);
            MarkerIsTreatedAsNote("dt", "DT", true);
            MarkerIsTreatedAsNote("ud", "UD", true);
            MarkerIsTreatedAsNote("dtb", "DTB", true);
            MarkerIsTreatedAsNote("chk", "CHK", true);
        }

        [Test]
        /// /s Heading [/s2] becomes /s2 Heading
        public void SBecomesS2WhenLabeledAtEnd()
        {
            string input = @"\id gen
\s Heading
\s Subheading [\s2]
\v 2
\vt text";
            string output = @"\id gen
\s1 Heading
\s2 Subheading 
\v 2
text";
            VerifyConversion(input, output);
        }

        /// Various markers have behavior similar to \bt.
        public void MarkerIsTreatedAsNote(string marker, string replacement, bool extraSpace)
        {
            string input =
                String.Format(
                    @"\id gen
\{0} text [\s2] more [\q3] and more \va and \ant yet
\{0} more\mr still \mr)more \lf special \ov)markers\ov]to covert \c 1",
                    marker);
            string output =
                String.Format(
                    @"\id gen
\note {0}: {1}text [s2] more [q3] and more variant and antonym yet\note*
\note {0}: {1}moremorphologystill morphology)more lexical function special older version)markersolder version]to covert \note*
\c 1",
                    replacement, (extraSpace ? " " : ""));
            VerifyConversion(input, output);
        }

        [Test]
        public void CanFixUdFollowedByChk()
        {
            string input = @"\id gen
\ud 12/12/45
\chk
\v 1";
            string output = @"\id gen
\note UD:  12/12/45\note*
\note CHK: \note*
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void CanFixARealFootnote()
        {
            string input =
                @"\id gen
\v 8
\vt Asaf|fn barana sang Yosafat,
\ft 1:8: Tulisan bahasa Yunani yang paling bae, tulis
|iAsaf|r. Ada tulisan saparu lai yang tulis, bilang,
|iAsa.|r
\v 9";
            string stage1Pattern =
                @"\id gen
\v 8
Asaf|fn barana sang Yosafat,
\f + \fr 1:8 \ft Tulisan bahasa Yunani yang paling bae, tulis{0}\it Asaf\it* . Ada tulisan saparu lai yang tulis, bilang,{0}\it Asa.\it* 
\f*
\v 9";

            string outputPattern =
                @"\id gen
\v 8
Asaf\f + \fr 1:8 \ft Tulisan bahasa Yunani yang paling bae, tulis{0}\it Asaf\it* . Ada tulisan saparu lai yang tulis, bilang,{0}\it Asa.\it* 
\f*  barana sang Yosafat,
\v 9";
            // The new conversion is slightly more aggressive about converting newlines to spaces.
            // In particular the ones before the |i are converted, because they aren't followed by
            // markers until later in the conversion.
            // If we change it to convert them sooner, other things break...in particular, it thinks
            // the footnote body terminates at the newline before the |i.
            string expectedStage1 = string.Format(stage1Pattern, "\r\n");
            string expectedStage1Re = string.Format(stage1Pattern, " ");
            string output = string.Format(outputPattern, "\r\n");
            string outputRe = string.Format(outputPattern, " ");
            string stage1 = ConvertCC(input);
            Assert.That(stage1, Is.EqualTo(expectedStage1));
            string ccResult = ConvertCC(stage1, @"c:/move_footnote_to_fn.cct");
            Assert.That(ccResult, Is.EqualTo(output));

            VerifyReConversion(input, expectedStage1Re);
            string reFinal = Convert(expectedStage1Re, GetTestFile("PostProcess.re"));
            Assert.That(reFinal, Is.EqualTo(outputRe));
        }

        /// <summary>
        /// Currently all the RE files we want to test must be copied to C:\testRe in order to be easily found.
        /// Todo: find some way to find them in their natural home under TestOwToUsfm/testRe
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetTestFile(string name)
        {
            return Path.Combine(@"C:\testRe", name);
        }

        [Test]
        public void Qh_ProducesLiWithNewline()
        {
            string input = @"\id gen
\qh some text
\v 1";
            string output = @"\id gen
\li
some text
\v 1";
            ;
            VerifyConversion(input, output);
        }

        [Test]
        public void MoveFootnoteToFn()
        {
            string input = @"\id gen
\vt This is some text with a |fn a note \ft marker : test
\vt more";
            string expectedStage1 = @"\id gen
This is some text with a |fn a note \f + \fr marker  \ft test
\f*
more";
            string output = @"\id gen
This is some text with a \f + \fr marker  \ft test
\f*  a note 
more";

            string stage1 = ConvertCC(input);
            Assert.That(stage1, Is.EqualTo(expectedStage1));
            string ccResult = ConvertCC(stage1, @"c:/move_footnote_to_fn.cct");
            Assert.That(ccResult, Is.EqualTo(output));

            VerifyReConversion(input, stage1);
            string reFinal = Convert(stage1, GetTestFile("PostProcess.re"));
            Assert.That(reFinal, Is.EqualTo(output));
        }

        [Test]
        public void MoveFootnoteToFnWithNewlineBeforeFt()
        {
            string input = @"\id gen
\vt This is some text with a |fn a note 
\ft marker : test
\vt more";
            string expectedStage1 =
                @"\id gen
This is some text with a |fn a note 
\f + \fr marker  \ft test
\f*
more";
            string output = @"\id gen
This is some text with a \f + \fr marker  \ft test
\f*  a note 
more";

            string stage1 = ConvertCC(input);
            Assert.That(stage1, Is.EqualTo(expectedStage1));
            string ccResult = ConvertCC(stage1, @"c:/move_footnote_to_fn.cct");
            Assert.That(ccResult, Is.EqualTo(output));

            VerifyReConversion(input, stage1);
            string reFinal = Convert(stage1, GetTestFile("PostProcess.re"));
            Assert.That(reFinal, Is.EqualTo(output));
        }

        /// <summary>
        /// Verifies stuff from the old file cleanup_OW_to_USFM.cct.
        /// Removes newlines before \x, \*x, \f, and \*f
        /// </summary>
        [Test]
        public void RemoveNewlinesBeforeXandF()
        {
            string input = @"\id gen
\vt This is some text
\x cross ref
\*x
\f something
\*f
\vt more";

            string output = @"\id gen
This is some text\x cross ref\*x\f something\*f
more";
            string stage1 = Convert(input);
            string reFinal = Convert(stage1, GetTestFile("PostProcess.re"));
            Assert.That(reFinal, Is.EqualTo(output));
        }

        private void VerifyConversion(string input, string expected)
        {
            // Old CC table sometimes produces double newlines, I think spuriously.
            VerifyCcConversion(input, expected);
            VerifyReConversion(input, expected);
        }

        private void VerifyCcConversion(string input, string expected)
        {
            string resultCC = ConvertCC(input).Replace("\r\n\r\n", "\r\n");
            Assert.That(resultCC, Is.EqualTo(expected));
        }

        private void VerifyReConversion(string input, string expected)
        {
            string result = Convert(input);
            Assert.That(result, Is.EqualTo(expected));
        }

        // TODO: Eradicate unsafe code.
        private unsafe string ConvertCC(string input)
        {
            return ConvertCC(input, @"c:/OW_To_PT.cct");
        }

        // TODO: Eradicate unsafe code.
        private unsafe string ConvertCC(string input, string tablePath)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            int cbyteInput = inputBytes.Length;
            // allocate a new buffer
            int nOutLen = Math.Max(10000, cbyteInput*6);
            var outBuffer = new byte[nOutLen];
            fixed (byte* lpOutBuffer = outBuffer)
            {
                lpOutBuffer[0] = lpOutBuffer[1] = lpOutBuffer[2] = lpOutBuffer[3] = 0;
                fixed (byte* lpInBuffer = inputBytes)
                {
                    Assert.That(File.Exists(tablePath));
                    Load(tablePath);
                    try
                    {
                        int* pnOut = &nOutLen;
                        {
                            var status = CCProcessBuffer(m_hTable, lpInBuffer, cbyteInput, lpOutBuffer, pnOut);
                            Assert.That(status, Is.EqualTo(0));
                        }
                    }
                    finally
                    {
                        Unload();
                    }

                }

            }
            // The CC table apparently strips \r but our test cases don't.
            return Encoding.UTF8.GetString(outBuffer, 0, nOutLen).Replace("\n", "\r\n");
        }

        private string Convert(string input)
        {
            return Convert(input, GetTestFile("OW_To_USFM.re"));
        }

        private string Convert(string input, string tablePath)
        {
            StreamReader tableReader = new StreamReader(tablePath, Encoding.UTF8);
            string temp = input;
            while (!tableReader.EndOfStream)
            {
                string source = tableReader.ReadLine();
                if (source.Trim().Length == 0)
                    continue;

                char delim = source[0];
                string[] parts = source.Split(new char[] {delim});
                string pattern = parts[1]; // parts[0] is the empty string before the first delimiter
                string replacement = parts[2];
                replacement = replacement.Replace("$r", "\r"); // Allow $r in replacement to become a true cr
                replacement = replacement.Replace("$n", "\n"); // Allow $n in replacement to become a true newline
                temp = System.Text.RegularExpressions.Regex.Replace(temp, pattern, replacement);
            }
            tableReader.Close();
            return temp;
        }

        private Int32 m_hTable = 0;

        protected unsafe int Load(string strTablePath)
        {
            // first make sure it's there
            if (!File.Exists(strTablePath))
                MessageBox.Show("Table " + strTablePath + "does not exist", "Error");
            Unload();
            Int32 hInstanceHandle = 0; // don't know what else to use here...
            byte[] baTablePath = Encoding.ASCII.GetBytes(strTablePath);
            fixed (byte* pszTablePath = baTablePath)
            fixed (Int32* phTable = &m_hTable)
            {
                int status = CCLoadTable(pszTablePath, phTable, hInstanceHandle);
                if (status != 0)
                {
                    Assert.Fail("bad status code from loading CC table " + status);
                }
                return status;
            }
        }

        protected bool IsFileLoaded()
        {
            return (m_hTable != 0);
        }

        protected void Unload()
        {
            if (IsFileLoaded())
            {
                CCUnloadTable(m_hTable);
                m_hTable = 0;
            }
        }

        /// <summary>
        /// The Kupang PreProcess step removes \nt fields, even multi-line ones.
        /// </summary>
        [Test]
        public void Preprocess_RemovesVariousMarkers()
        {
            string input =
                @"\nt first note
\id gen
\vt This is some text 
\nt This should go
\vt more
\nt This should go too
and so should this
and also this
\ntgt Consecutive ones should go, even with longer markers
\ov also not wanted \lf
even on multiple lines
\vt but this should survive
\nt last note
ends here";

            string expected = @"\id gen
\vt This is some text
\vt more
\vt but this should survive
";

            string output = Convert(input, GetTestFile("PreProcess.re")).Replace("\n", "\r\n");
            Assert.That(output, Is.EqualTo(expected));
        }

        #region DLLImport Statements

        [DllImport("Cc32", SetLastError = true)]
        private static extern unsafe int CCLoadTable(byte* lpszCCTableFile,
                                                     Int32* hpLoadHandle,
                                                     Int32 hinstCurrent);

        [DllImport("Cc32", SetLastError = true)]
        private static extern unsafe int CCUnloadTable(Int32 hUnlHandle);

        [DllImport("Cc32", SetLastError = true)]
        private static extern unsafe int CCProcessBuffer(Int32 hProHandle,
                                                         byte* lpInputBuffer, int nInBufLen,
                                                         byte* lpOutputBuffer, int* npOutBufLen);

        #endregion DLLImport Statements
    }
}

