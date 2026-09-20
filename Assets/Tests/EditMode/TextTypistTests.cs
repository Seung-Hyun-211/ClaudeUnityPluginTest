using NUnit.Framework;
using Game.Dialogue;

namespace Game.Tests
{
    public class TextTypistTests
    {
        private static TextTypist Started(int total, params TextPause[] pauses)
        {
            var typist = new TextTypist();
            typist.Reset(total, pauses);
            return typist;
        }

        [Test]
        public void Reveals_CharactersAtTheGivenRate_AndFinishes()
        {
            var typist = Started(10);

            typist.Advance(0.1f, 40f);
            Assert.AreEqual(4, typist.VisibleCount);
            Assert.IsFalse(typist.IsDone);

            typist.Advance(1f, 40f);
            Assert.AreEqual(10, typist.VisibleCount);
            Assert.IsTrue(typist.IsDone);
        }

        [Test]
        public void Pause_HoldsTheTypingAtItsIndex_ThenResumes()
        {
            var typist = Started(10, new TextPause(4, 0.5f));

            typist.Advance(0.7f, 10f); // reaches index 4 after 0.4s, 0.3s of the 0.5s pause used
            Assert.AreEqual(4, typist.VisibleCount);

            typist.Advance(0.05f, 10f);
            Assert.AreEqual(4, typist.VisibleCount);

            typist.Advance(0.5f, 10f); // 0.15s finishes the pause, 0.35s types 3 more
            Assert.AreEqual(7, typist.VisibleCount);
        }

        [Test]
        public void Pause_ConsumesItsFullDuration_BeforeMoreCharactersShow()
        {
            var typist = Started(10, new TextPause(2, 1f));

            typist.Advance(0.2f, 10f);          // exactly at the pause
            typist.Advance(0.99f, 10f);         // still inside it
            Assert.AreEqual(2, typist.VisibleCount);

            typist.Advance(0.02f + 0.3f, 10f);  // leaves the pause and types 3 more
            Assert.AreEqual(5, typist.VisibleCount);
        }

        [Test]
        public void Pause_AtTheStart_DelaysTheFirstCharacter()
        {
            var typist = Started(5, new TextPause(0, 0.5f));

            typist.Advance(0.4f, 10f);
            Assert.AreEqual(0, typist.VisibleCount);

            typist.Advance(0.3f, 10f);
            Assert.AreEqual(2, typist.VisibleCount);
        }

        [Test]
        public void Pause_AtTheEnd_DelaysDone()
        {
            var typist = Started(3, new TextPause(3, 0.5f));

            typist.Advance(0.4f, 10f);
            Assert.AreEqual(3, typist.VisibleCount);
            Assert.IsFalse(typist.IsDone);

            typist.Advance(0.5f, 10f);
            Assert.IsTrue(typist.IsDone);
        }

        [Test]
        public void Complete_ShowsEverythingAndSkipsPauses()
        {
            var typist = Started(10, new TextPause(4, 5f));

            typist.Complete();

            Assert.AreEqual(10, typist.VisibleCount);
            Assert.IsTrue(typist.IsDone);
        }

        [Test]
        public void ZeroRate_DoesNotAdvance_AndDoesNotHang()
        {
            var typist = Started(10);

            typist.Advance(5f, 0f);

            Assert.AreEqual(0, typist.VisibleCount);
        }

        [Test]
        public void EmptyText_IsDoneImmediately()
        {
            Assert.IsTrue(Started(0).IsDone);
        }

        [Test]
        public void Reset_StartsOver()
        {
            var typist = Started(3);
            typist.Complete();

            typist.Reset(8, null);

            Assert.AreEqual(0, typist.VisibleCount);
            Assert.IsFalse(typist.IsDone);
        }
    }
}
