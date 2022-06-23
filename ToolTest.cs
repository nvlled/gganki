
using Love;
using gganki_love;
using Xunit;
using AwaitableCoroutine;
using System.Threading;
using System.Text.Json;

public class ToolTest
{
    //[Fact]
    public async void TestFetchDecks()
    {
        var deckName = "AJT Kanji Transition TSC";
        var deck = await AnkiConnect.FetchAllCards(deckName);
        var decks = await AnkiConnect.FetchDecks();
        //var deck = await AnkiConnect.FetchAvailableCards("AJT Kanji Transition TSC");
        Console.WriteLine(deck.error);
        //var card = deck.value[0];
        var card = deck.value[^1];

        foreach (var entry in decks.value)
        {
            Console.WriteLine(entry);
        }

        var cardResp = await AnkiConnect.FetchCardInfo(card.cardId);
        cardResp.value[0].css = "";
        cardResp.value[0].answer = "";
        cardResp.value[0].question = "";
        PrintVar(cardResp.value);


        var deckConfig = await AnkiConnect.FetchDeckConfig(deckName);
        Console.WriteLine(JsonSerializer.Serialize(deckConfig, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("----------------------------");



        var setEaseResp = await AnkiConnect.AnswerCard(card.cardId, AnkiButton.Good);
        Console.WriteLine("answer card response: {0}", JsonSerializer.Serialize(setEaseResp));

        //Console.WriteLine($"factor={card.factor}");
        //var json = await AnkiConnect.FetchJSON("getEaseFactors", new
        //{
        //    cards = new ulong[] { card.cardId },
        //});
        //Console.WriteLine($"{card.GetVocab()} | factor={json}");

        Console.WriteLine("----------------------------");
        cardResp = await AnkiConnect.FetchCardInfo(card.cardId);
        cardResp.value[0].css = "";
        cardResp.value[0].answer = "";
        cardResp.value[0].question = "";
        PrintVar(cardResp.value);
    }

}