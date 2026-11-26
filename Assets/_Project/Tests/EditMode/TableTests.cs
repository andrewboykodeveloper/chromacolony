using System;
using System.Collections.Generic;
using Chromacolony.Core;
using NUnit.Framework;

namespace Chromacolony.Tests.EditMode
{
    public sealed class TableTests
    {
        [Test]
        public void CsvReadsQuotedFieldsWithCommasQuotesAndLineBreaks()
        {
            List<List<string>> rows = Csv.Parse("key,en\nbuy,\"{0}, for \"\"{1}\"\"\"\nnote,\"two\nlines\"\n");

            Assert.That(rows, Has.Count.EqualTo(3));
            Assert.That(rows[1], Is.EqualTo(new[] { "buy", "{0}, for \"{1}\"" }));
            Assert.That(rows[2], Is.EqualTo(new[] { "note", "two\nlines" }));
        }

        [Test]
        public void CsvSkipsAByteOrderMarkAndCarriageReturns()
        {
            List<List<string>> rows = Csv.Parse("\uFEFFkey,value\r\nslots,5\r\n");

            Assert.That(rows[0], Is.EqualTo(new[] { "key", "value" }));
            Assert.That(rows[1], Is.EqualTo(new[] { "slots", "5" }));
        }

        [Test]
        public void CsvRejectsAQuoteThatIsNeverClosed()
        {
            Assert.Throws<FormatException>(() => Csv.Parse("key,value\nname,\"open\n"));
        }

        [Test]
        [SetCulture("de-DE")]
        public void ConfigValuesAreReadWithTheInvariantCulture()
        {
            var config = new ConfigTable("key,value\nslots,5\ncrab_interval,0.2\ncolor,E8584A\n");

            Assert.That(config.Int("slots"), Is.EqualTo(5));
            Assert.That(config.Float("crab_interval"), Is.EqualTo(0.2f));
            Assert.That(config.String("color"), Is.EqualTo("E8584A"));
        }

        [Test]
        public void AMissingConfigKeyFailsLoudly()
        {
            var config = new ConfigTable("key,value\nslots,5\n");

            Assert.Throws<KeyNotFoundException>(() => config.Int("missing"));
        }

        [Test]
        public void AMissingTextFallsBackToTheFirstLanguageAndAMissingKeyShowsItself()
        {
            var text = new Localization("key,en,ru\ntitle,Chromacolony,Хромоколония\nplay,Play,\n", "ru");

            Assert.That(text.Language, Is.EqualTo("ru"));
            Assert.That(text.Get("title"), Is.EqualTo("Хромоколония"));
            Assert.That(text.Get("play"), Is.EqualTo("Play"));
            Assert.That(text.Get("nothing"), Is.EqualTo("[nothing]"));
        }

        [Test]
        public void AnUnknownLanguageFallsBackToTheFirstOne()
        {
            var text = new Localization("key,en,ru\nboard,Board {0},Доска {0}\n", "de");

            Assert.That(text.Language, Is.EqualTo("en"));
            Assert.That(text.Format("board", 3), Is.EqualTo("Board 3"));
        }

        [Test]
        public void TheTextsChangeTheirLanguageInPlace()
        {
            var text = new Localization("key,en,ru\nboard,Board {0},Доска {0}\n", "en");

            text.Use("ru");
            Assert.That(text.Language, Is.EqualTo("ru"));
            Assert.That(text.Format("board", 3), Is.EqualTo("Доска 3"));

            text.Use("de");
            Assert.That(text.Language, Is.EqualTo("en"));
            Assert.That(text.Format("board", 3), Is.EqualTo("Board 3"));
        }
    }
}
