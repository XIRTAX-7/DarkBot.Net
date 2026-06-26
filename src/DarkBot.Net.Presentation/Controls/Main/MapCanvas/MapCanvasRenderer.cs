using DarkBot.Net.Presentation.Models.Main;
using DarkBot.Net.Presentation.Models.Main.Map;
using SkiaSharp;

namespace DarkBot.Net.Presentation.Controls.Main.MapCanvas;

internal static class MapCanvasRenderer
{
  private const int DefaultMapWidth = 21000;
  private const int DefaultMapHeight = 13500;

  public static void Render(
      SKCanvas canvas,
      int width,
      int height,
      BotUiSnapshot? snapshot,
      IReadOnlyList<HeroTrailPoint> heroTrail,
      MapMoveTarget? moveTarget,
      long renderTimestamp,
      long trailLifetimeTicks)
  {
      var map = snapshot?.Map ?? MapRenderSnapshot.Loading;
      var mapWidth = Math.Max(map.MapWidth > 0 ? map.MapWidth : DefaultMapWidth, 1);
      var mapHeight = Math.Max(map.MapHeight > 0 ? map.MapHeight : DefaultMapHeight, 1);

      var ctx = new MapCanvasRenderContext
      {
          Canvas = canvas,
          Transform = MapViewTransform.Create(new System.Windows.Size(width, height), mapWidth, mapHeight),
          Width = width,
          Height = height,
          Map = map with
          {
              MapWidth = mapWidth,
              MapHeight = mapHeight
          },
          HeroTrail = heroTrail,
          MoveTarget = moveTarget,
          RenderTimestamp = renderTimestamp,
          TrailLifetimeTicks = trailLifetimeTicks
      };

      MapCanvasBackgroundDrawer.Draw(ctx);
      MapCanvasZonesDrawer.Draw(ctx);
      MapCanvasInfosDrawer.Draw(ctx);
      MapCanvasHeroDrawer.DrawTrail(ctx);
      MapCanvasEntitiesDrawer.DrawConstant(ctx);
      MapCanvasEntitiesDrawer.DrawDynamic(ctx);
      MapCanvasHeroDrawer.DrawConfiguration(ctx);
      MapCanvasHeroDrawer.DrawHeroAndPet(ctx);
      MapCanvasDevDrawer.Draw(ctx);
      MapCanvasOverlayDrawer.Draw(ctx);
  }
}
