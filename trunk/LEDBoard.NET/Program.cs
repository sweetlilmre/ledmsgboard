using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LEDBoardLib;
using System.Threading;

namespace LEDBoardNET
{
  class Program
  {
    static void Main(string[] args)
    {
      Program p = new Program();
      p.DoPong();
    }

    private int ballOldx = 0, ballOldy = 0;
    private int ballx = 0, bally = 0;
    private int ballDx = 1, ballDy = 1;


    public void DoPong()
    {
      LEDBoard board = new LEDBoard(Default5x7font.Font);

      board.Init();
      int leftPaddleY = 2;

      int x = 0, dx = 1;
      while (true)
      {
        //board.DrawChar('h', 0, 0);
        board.DrawString("hello", x, 0);
        board.Update();
        Thread.Sleep(50);
        x = x + dx;
        if (x > 10)
        {
          x = 10;
          dx = -1;

        }
        if (x < -5)
        {
          x = -5;
          dx = 1;
        }
      }
    }

    private void drawPaddle(LEDBoardLib.LEDBoard board, bool left, int y)
    {
      for (int n = 0; n < 7; n++)
      {
        board.ClearPixel(left ? 0 : 20, n);
      }
      for(int n = 0; n < 3; n++)
      {
        board.SetPixel(left ? 0 : 20, n + y);
      }
    }

    private void drawBall(LEDBoardLib.LEDBoard board)
    {
      board.ClearPixel(ballOldx, ballOldy);
      ballx += ballDx;
      bally += ballDy;
      if (bally < 0)
      {
        ballDy = 1;
        bally = 0;
      }
      if (bally > 6)
      {
        ballDy = -1;
        bally = 6;
      }

      board.SetPixel(ballx, bally);
      ballOldx = ballx;
      ballOldy = bally;
    }
  }
}
