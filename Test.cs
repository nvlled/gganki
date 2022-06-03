

using Love;
using gganki_love;
using Xunit;

public class Test
{
	//[Fact]
	public async void TestFetchDecks()
	{
		var deckResp = await AnkiConnect.FetchDecks();
		Assert.NotNull(deckResp);

		if (deckResp?.value?.Count() == 0)
		{
			Console.WriteLine("no decks available");
			return;
		}

		var deckName = deckResp?.value?.Keys.First() ?? "";
		deckName = "AJT Kanji Transition TSC";

		Console.WriteLine("using deck: " + deckName);
		var cardIdResp = await AnkiConnect.FetchAvailableCardIds(deckName);
		Assert.NotNull(cardIdResp);
		Assert.Null(cardIdResp.error);
		Assert.NotEmpty(cardIdResp.value);


		var cardResp = await AnkiConnect.FetchCardInfo(cardIdResp?.value?[1] ?? 0);
		Assert.NotNull(cardResp);
		Assert.NotEmpty(cardResp.value);
		Console.WriteLine(cardResp?.value?[0].cardId);
	}

	//[Fact]
	public async void TestFetchCardInfo()
	{
		var resp = await AnkiConnect.FetchCardInfo(1614446068564);
		Assert.NotNull(resp);
		Assert.Null(resp.error);
		Assert.NotEmpty(resp.value);
	}


	class Item : IPos
	{
		public string name { set; get; }
		public Vector2 pos { get; set; }
		//public Vector2 lastPos { get; set; }

		public override string ToString()
		{
			return string.Format("[{0} {1}]", name, pos);
		}
	}

	//[Fact]
	public async void TestPartitionList()
	{
		var list = new PartitionedList<Item>(10);
		var a = new Item { name = "a" };
		var b = new Item { name = "b", pos = new Vector2(150, 0) };
		var c = new Item { name = "c", pos = new Vector2(5, 5) };

		list.Add(a);
		list.Add(b);
		list.Add(c);


		for (var i = 0; i < 10; i++)
		{
			foreach (var x in list.Iterate())
			{
				x.pos += new Vector2(1.1f, 1.05f);
				list.Move(x);
			}
		}


		var partition = list.GetItemsAt(new Vector2(11, 19)).ToHashSet();
		Assert.True(partition.Contains(a));
		Assert.False(partition.Contains(b));
		Assert.True(partition.Contains(c));
	}

	//[Fact]
	public async void TestLoadAudio()
	{

		var filename = "78de88070e17b513462f962a8a481c6d.ogg";
		var source = await AudioManager.LoadAudio(filename);
		//source.Play();
	}

	[Fact]
	public async void TestJPSplit()
	{
		var s = "aa毎bb日h、commaジョギンThe lazy brown fox fell over a hole.グをしています。 test";
		Console.WriteLine("s={0}", s);
		Console.WriteLine("wut {0}", JP.SplitText(s).ToArray().Length);
		foreach (var entry in JP.SplitText(s))
		{
			Console.WriteLine("> {0} | {1},{2}", entry.text, entry.type, entry.kIndex);
		}
	}
}
