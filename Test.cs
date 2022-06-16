

using Love;
using gganki_love;
using Xunit;
using AwaitableCoroutine;
using System.Threading;

public class Test
{

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

    //[Fact]
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


    public async Coroutine SubSubRoutine(CoroutineControl ctrl)
    {

        int i = 0;
        //await Coroutine.While(() => i < 100).UntilCompleted(() =>
        //{
        //    Console.WriteLine(++i);
        //});
        while (true)
        {
            Console.WriteLine(++i);
            await ctrl.Yield();
        }
        await ctrl.Abyss();
    }
    public async Coroutine SubRoutine(CoroutineControl ctrl)
    {
        await SubSubRoutine(ctrl);
        await ctrl.Abyss();
    }

    //[Fact]
    public void TestCoroutine()
    {
        var runner = new CoroutineRunner();
        var a = runner.Create(async () =>
        {
            while (true)
            {
                Console.WriteLine("starting");
                var ctrl = new CoroutineControl();
                var co = SubRoutine(ctrl);
                await Coroutine.DelayCount(10);
                //co.Cancel();
                //await co.UntilCompleted(() =>
                //{
                //    co.Cancel();
                //});
                ctrl.Cancel(co);
                Console.WriteLine("cancelled");
            }
        });
        Console.WriteLine("a");
        //a.Cancel();

        while (true)
        {
            runner.Update();
            Thread.Sleep(250);
        }
    }

    [Fact]
    public void TestAwaitable()
    {
        var runner = new CoroutineRunner();
        var co = runner.Create(async () =>
        {
            try
            {

                await Fn();
            }
            finally
            {
                Console.WriteLine("finally A");
            }
        });

        while (!co.IsCompleted)
        {
            runner.Update();
        }
    }

    public async Coroutine Fn()
    {
        try
        {
            await Coroutine.WaitAny(
                Fn3(),
                Fn2()
            );
        }
        finally
        {
            Console.WriteLine("finally B");
        }
    }
    public async Coroutine Fn2()
    {
        try
        {
            await Coroutine.DelayCount(300);
            throw new Exception();
        }
        finally
        {
            Console.WriteLine("finally C");
        }
    }

    public async Coroutine Fn3()
    {
        using var _ = Defer.Run(() =>
        {
            Console.WriteLine("finally D");
        });
        await Coroutine.DelayCount(100);
    }

}
