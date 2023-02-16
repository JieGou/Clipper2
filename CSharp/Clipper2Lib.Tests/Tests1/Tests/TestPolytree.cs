using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Clipper2Lib.UnitTests
{
  [TestClass]
  public class TestPolytree
  {
    private void PolyPathContainsPoint(PolyPath64 pp, Point64 pt, ref int counter)
    {
      if (Clipper.PointInPolygon(pt, pp.Polygon!) != PointInPolygonResult.IsOutside)
      {
        if (pp.IsHole) --counter; else ++counter;
      }
      for (int i = 0; i < pp.Count; i++)
      {
        PolyPath64 child = (PolyPath64) pp[i];
        PolyPathContainsPoint(child, pt, ref counter);
      }
    }

    private bool PolytreeContainsPoint(PolyTree64 pp, Point64 pt)
    {
      int counter = 0;
      for (int i = 0; i < pp.Count; i++)
      {
        PolyPath64 child = (PolyPath64) pp[i];
        PolyPathContainsPoint(child, pt, ref counter);
      }
      Assert.IsTrue(counter >= 0, "Polytree has too many holes");
      return counter != 0;
    }

    private bool PolyPathFullyContainsChildren(PolyPath64 pp)
    {
      foreach (PolyPath64 child in pp.Cast<PolyPath64>())
      {
        foreach (Point64 pt in child.Polygon!)
          if (Clipper.PointInPolygon(pt, pp.Polygon!) == PointInPolygonResult.IsOutside)
            return false;
        if (child.Count > 0 && !PolyPathFullyContainsChildren(child))
          return false;
      }
      return true;
    }

    private bool CheckPolytreeFullyContainsChildren(PolyTree64 polytree)
    {
      for (int i = 0; i < polytree.Count; i++)
      {
        PolyPath64 child = (PolyPath64) polytree[i];
        if (child.Count > 0 && !PolyPathFullyContainsChildren(child))
          return false;
      }
      return true;
    }

    [TestMethod]
    public void TestPolytree2()
    {
      Paths64 subject = new(), subjectOpen = new(), clip = new();

      Assert.IsTrue(ClipperFileIO.LoadTestNum("..\\..\\..\\..\\..\\..\\Tests\\PolytreeHoleOwner2.txt",
        1, subject, subjectOpen, clip, out ClipType cliptype, out FillRule fillrule,
        out _, out _, out _),
          "Unable to read PolytreeHoleOwner2.txt");

      PolyTree64 solutionTree = new();
      Paths64 solution_open = new();
      Clipper64 clipper = new();

      Path64 pointsOfInterestOutside = new()
      {
        new Point64(21887, 10420),
        new Point64(21726, 10825),
        new Point64(21662, 10845),
        new Point64(21617, 10890)
      };

      foreach (Point64 pt in pointsOfInterestOutside)
      {
        foreach (Path64 path in subject)
        {
          Assert.IsTrue(Clipper.PointInPolygon(pt, path) == PointInPolygonResult.IsOutside,
            "outside point of interest found inside subject");
        }
      }

      Path64 pointsOfInterestInside = new()
      {
        new Point64(21887, 10430),
        new Point64(21843, 10520),
        new Point64(21810, 10686),
        new Point64(21900, 10461)
      };

      foreach (Point64 pt in pointsOfInterestInside)
      {
        int poi_inside_counter = 0;
        foreach (Path64 path in subject)
        {
          if (Clipper.PointInPolygon(pt, path) == PointInPolygonResult.IsInside)
            poi_inside_counter++;
        }
        Assert.IsTrue(poi_inside_counter == 1,
          string.Format("poi_inside_counter - expected 1 but got {0}", poi_inside_counter));
      }

      clipper.AddSubject(subject);
      clipper.AddOpenSubject(subjectOpen);
      clipper.AddClip(clip);
      clipper.Execute(cliptype, fillrule, solutionTree, solution_open);

      Paths64 solutionPaths = Clipper.PolyTreeToPaths64(solutionTree);
      double a1 = Clipper.Area(solutionPaths), a2 = solutionTree.Area();

      Assert.IsTrue(a1 > 330000,
        string.Format("solution has wrong area - value expected: 331,052; value returned; {0} ", a1));

      Assert.IsTrue(Math.Abs(a1 - a2) < 0.0001,
        string.Format("solution tree has wrong area - value expected: {0}; value returned; {1} ", a1, a2));

      Assert.IsTrue(CheckPolytreeFullyContainsChildren(solutionTree),
        "The polytree doesn't properly contain its children");

      foreach (Point64 pt in pointsOfInterestOutside)
        Assert.IsFalse(PolytreeContainsPoint(solutionTree, pt),
          "The polytree indicates it contains a point that it should not contain");

      foreach (Point64 pt in pointsOfInterestInside)
        Assert.IsTrue(PolytreeContainsPoint(solutionTree, pt),
          "The polytree indicates it does not contain a point that it should contain");
    }

    //测试例子 From https://documentation.help/The-Clipper-Library/_Body5.htm
    /// <summary>
    /// 测试Clipper多边形-带洞口和孤岛
    /// </summary>
    [TestMethod]
    public void TestPolytreeHoleAndIsland()
    {
      //用面积来判断 断言面积=4000
      double area = 4000.0;

      Paths64 subject = new()
      {
        Clipper.MakePath(new int[] { 10, 10, 100, 10, 100, 100, 10, 100 })
      };
      Paths64 clip = new()
      {
        Clipper.MakePath(new int[] { 20, 20, 20, 90, 90, 90, 90, 20 }),
        Clipper.MakePath(new int[] { 30, 30, 50, 30, 50, 50, 30, 50 }),
        Clipper.MakePath(new int[] { 60, 60, 80, 60, 80, 80, 60, 80 })
      };

      ClipType cliptype = ClipType.Difference;
      FillRule fillRule = FillRule.EvenOdd;

      Clipper64 clipper = new();
      clipper.AddSubject(subject);
      clipper.AddClip(clip);
      PolyTree64 solutionTree = new();

      clipper.Execute(cliptype, fillRule, solutionTree);

      double calculateArea = solutionTree.Area();
      Assert.IsTrue(Math.Abs(area - calculateArea) < 0.0001,
                    string.Format("带洞口和孤岛的真实面积为: {0}; 计算返回值: {1} ", area, calculateArea));
    }

    /// <summary>
    /// 测试Clipper多边形-带洞口
    /// </summary>
    [TestMethod]
    public void TestPolytreeHole()
    {
      double area = 3200.0;

      Paths64 subject = new()
      {
        Clipper.MakePath(new int[] { 10, 10, 100, 10, 100, 100, 10, 100 })
      };
      Paths64 clip = new()
      {
        Clipper.MakePath(new int[] { 20, 20, 20, 90, 90, 90, 90, 20 }),
      };

      ClipType cliptype = ClipType.Difference;
      FillRule fillRule = FillRule.EvenOdd;

      Clipper64 clipper = new();
      clipper.AddSubject(subject);
      clipper.AddClip(clip);
      PolyTree64 solutionTree = new();

      clipper.Execute(cliptype, fillRule, solutionTree);

      double calculateArea = solutionTree.Area();
      Assert.IsTrue(Math.Abs(area - calculateArea) < 0.0001,
                    string.Format("带洞口和孤岛的真实面积为: {0}; 计算返回值: {1} ", area, calculateArea));
    }

    //Note 处理楼板的逻辑 只可能是相离或者是洞口，不可能相交
    //<image url="$(ProjectDir)\DocumentImages\Revit_FloorWithHole.png"/>
    /// <summary>
    /// 测试Clipper多边形带洞口_对应Revit楼板
    /// </summary>
    [TestMethod]
    public void TestPolytreeHole_RevitFloor()
    {
      double area = 2 * 48 * 1E6;

      var path1 = Clipper.MakePath(new int[] { 0, 0, 10000, 0, 10000, 5000, 0, 5000 });
      var path2 = Clipper.MakePath(new int[] { 11000, 0, 21000, 0, 21000, 5000, 11000, 5000 });
      var path3 = Clipper.MakePath(new int[] { 4000, 2000, 6000, 2000, 6000, 3000, 4000, 3000 });
      var path4 = Clipper.MakePath(new int[] { 15000, 2000, 17000, 2000, 17000, 3000, 15000, 3000 });
      //先按面积大小排序
      var pathList = new List<Path64>() { path1, path2, path3, path4, };
      var sortedPaths = pathList.OrderByDescending(p => Clipper.Area(p))
                        .ToList();

      Paths64 subject = new();

      sortedPaths.ForEach(p => subject.Add(p));

      ClipType cliptype = ClipType.Difference;
      FillRule fillRule = FillRule.EvenOdd;

      Clipper64 clipper = new();
      clipper.AddSubject(subject);

      PolyTree64 solutionTree = new();

      clipper.Execute(cliptype, fillRule, solutionTree);

      double calculateArea = solutionTree.Area();
      Assert.IsTrue(Math.Abs(area - calculateArea) < 0.0001,
                    string.Format("带洞口和孤岛的真实面积为: {0}; 计算返回值: {1} ", area, calculateArea));
    }
  }
}