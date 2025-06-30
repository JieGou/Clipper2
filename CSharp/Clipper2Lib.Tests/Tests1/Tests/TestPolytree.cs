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

    /// <summary>
    /// 测试Clipper多边形带洞口_通过扩大构造外圈来实现
    /// </summary>
    [TestMethod]
    public void TestPolytreeHole_ConstructOuter()
    {
      double area = 2 * 48 * 1E6;

      var path1 = Clipper.MakePath(new int[] { 0, 0, 10000, 0, 10000, 5000, 0, 5000 });
      var path2 = Clipper.MakePath(new int[] { 11000, 0, 21000, 0, 21000, 5000, 11000, 5000 });
      var path3 = Clipper.MakePath(new int[] { 4000, 2000, 6000, 2000, 6000, 3000, 4000, 3000 });
      var path4 = Clipper.MakePath(new int[] { 15000, 2000, 17000, 2000, 17000, 3000, 15000, 3000 });
      //先按面积大小排序
      var pathList = new List<Path64>() { path1, path2, path3, path4, };

      var paths = new Paths64(pathList);
      //扩大作为subject 其它全部放在 clip里
      var rec = Clipper.GetBounds(paths);
      var recPath = rec.AsPath();
      var offsetD = Math.Sqrt(Math.Pow(rec.Width, 2) + Math.Pow(rec.Height, 2)) * 0.1;
      //向外偏移
      var offsetPaths = Clipper.InflatePaths(new Paths64() { recPath }, offsetD, JoinType.Miter, EndType.Polygon, 10);

      Paths64 clip = new();
      pathList.ForEach(p => clip.Add(p));

      ClipType cliptype = ClipType.Difference;
      FillRule fillRule = FillRule.EvenOdd;

      Clipper64 clipper = new();
      clipper.AddSubject(offsetPaths);
      clipper.AddClip(clip);

      PolyTree64 solutionTree = new();
      clipper.Execute(cliptype, fillRule, solutionTree);
      var level = solutionTree.Level;

      var holeList = new List<Path64>();
      //第一层按上述扩大算法必定只有一个
      PolyPath64? childTree = solutionTree.Child(0);
      PolyTreeRecursiveIterationHole(childTree, holeList);

      double calculateArea = solutionTree.Area();
      Assert.IsTrue(Math.Abs(area - calculateArea) < 0.0001,
                    string.Format("带洞口和孤岛的真实面积为: {0}; 计算返回值: {1} ", area, calculateArea));
    }

    /// <summary>
    /// 递归遍历 PolyTree 中的洞口Hole
    /// </summary>
    /// <param name="dir"></param>
    /// <param name="holeList"></param>
    public void PolyTreeRecursiveIterationHole(PolyPath64 polyTree, List<Path64> holeList)
    {
      var count = polyTree.Count;
      for (int i = 0; i < count; i++)
      {
        var child = polyTree.Child(i);
        if (child.Count > 1)
        {
          PolyTreeRecursiveIterationHole(child, holeList);
        }
        else
        {
          if (child.Count == 1)
          {
            if (child.IsHole)
            {
              if (child.Polygon != null)
              {
                holeList.Add(child.Polygon);
              }
            }
          }
        }
      }
    }

    [TestMethod]
    public void TestPolytree3()
    {
      Paths64 subject = new();
      subject.Add(Clipper.MakePath(new int[] {1588700, -8717600, 
        1616200, -8474800, 1588700, -8474800 }));
      subject.Add(Clipper.MakePath(new int[] { 13583800,-15601600, 
        13582800,-15508500, 13555300,-15508500, 13555500,-15182200, 
        13010900,-15185400 }));
      subject.Add(Clipper.MakePath(new int[] { 956700, -3092300, 1152600, 
        3147400, 25600, 3151700 }));
      subject.Add(Clipper.MakePath(new int[] { 
        22575900,-16604000, 31286800,-12171900,
        31110200,4882800, 30996200,4826300, 30414400,5447400, 30260000,5391500,
        29662200,5805400, 28844500,5337900, 28435000,5789300, 27721400,5026400,
        22876300,5034300, 21977700,4414900, 21148000,4654700, 20917600,4653400,
        19334300,12411000, -2591700,12177200, 53200,3151100, -2564300,12149800,
        7819400,4692400, 10116000,5228600, 6975500,3120100, 7379700,3124700,
        11037900,596200, 12257000,2587800, 12257000,596200, 15227300,2352700,
        18444400,1112100, 19961100,5549400, 20173200,5078600, 20330000,5079300,
        20970200,4544300, 20989600,4563700, 19465500,1112100, 21611600,4182100,
        22925100,1112200, 22952700,1637200, 23059000,1112200, 24908100,4181200,
        27070100,3800600, 27238000,3800700, 28582200,520300, 29367800,1050100,
        29291400,179400, 29133700,360700, 29056700,312600, 29121900,332500,
        29269900,162300, 28941400,213100, 27491300,-3041500, 27588700,-2997800,
        22104900,-16142800, 13010900,-15603000, 13555500,-15182200,
        13555300,-15508500, 13582800,-15508500, 13583100,-15154700,
        1588700,-8822800, 1588700,-8379900, 1588700,-8474800, 1616200,-8474800,
        1003900,-630100, 1253300,-12284500, 12983400,-16239900}));
      subject.Add(Clipper.MakePath(new int[] { 198200, 12149800, 1010600, 12149800, 1011500, 11859600 }));
      subject.Add(Clipper.MakePath(new int[] { 21996700, -7432000, 22096700, -7432000, 22096700, -7332000 }));
      PolyTree64 solutionTree = new();

      Clipper64 clipper = new();
      clipper.AddSubject(subject);
      clipper.Execute(ClipType.Union, FillRule.NonZero, solutionTree);

      Assert.IsTrue(solutionTree.Count == 1 && solutionTree[0].Count == 2
        && solutionTree[0][1].Count == 1, "Incorrect PolyTree nesting.");


    } // end TESTMETHOD TestPolytree3

  } // end TestClass

}