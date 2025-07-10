using CMS.ContentEngine;
using XperienceCommunity.DataContext.Contexts;

namespace XperienceCommunity.DataContext.Tests;

public class ExpressionContextTests
{
    private class DummyWhereParameters { }

    [Fact]
    public void AddParameter_AddsParameter_WhenNotExists()
    {
        var ctx = new ExpressionContext();
        ctx.AddParameter("foo", 123);

        Assert.True(ctx.Parameters.ContainsKey("foo"));
        Assert.Equal(123, ctx.Parameters["foo"]);
    }

    [Fact]
    public void AddParameter_DoesNotOverwrite_ExistingParameter()
    {
        var ctx = new ExpressionContext();
        ctx.AddParameter("foo", 123);
        ctx.AddParameter("foo", 456);

        Assert.Equal(123, ctx.Parameters["foo"]);
    }

    [Fact]
    public void PushAndPopMember_TracksMemberAccessChain()
    {
        var ctx = new ExpressionContext();
        ctx.PushMember("A");
        ctx.PushMember("B");

        Assert.Equal(new[] { "B", "A" }, ctx.MemberAccessChain);

        var popped = ctx.PopMember();
        Assert.Equal("B", popped);
        Assert.Equal(new[] { "A" }, ctx.MemberAccessChain);
    }

    [Fact]
    public void PushAndPopLogicalGrouping_TracksLogicalGroupings()
    {
        var ctx = new ExpressionContext();
        ctx.PushLogicalGrouping("AND");
        ctx.PushLogicalGrouping("OR");

        Assert.Equal(new[] { "OR", "AND" }, ctx.LogicalGroupings);

        var popped = ctx.PopLogicalGrouping();
        Assert.Equal("OR", popped);
        Assert.Equal(new[] { "AND" }, ctx.LogicalGroupings);
    }

    [Fact]
    public void AddWhereAction_AddsAction()
    {
        var ctx = new ExpressionContext();
        bool called = false;
        Action<WhereParameters> action = _ => called = true;

        ctx.AddWhereAction(action);

        Assert.Single(ctx.WhereActions);
        ctx.WhereActions[0].Invoke(null!);
        Assert.True(called);
    }

    [Fact]
    public void Clear_ResetsAllState()
    {
        var ctx = new ExpressionContext();
        ctx.AddParameter("foo", 1);
        ctx.PushMember("A");
        ctx.PushLogicalGrouping("AND");
        ctx.AddWhereAction(_ => { });

        ctx.Clear();

        Assert.Empty(ctx.Parameters);
        Assert.Empty(ctx.MemberAccessChain);
        Assert.Empty(ctx.LogicalGroupings);
        Assert.Empty(ctx.WhereActions);
    }
}
