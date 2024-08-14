using System.Collections.Generic;
using UnityEngine;

namespace ReflectionRazor
{
	/// <summary>
	/// グリッド状のフィールドを表現する
	/// </summary>
	public class GridField
	{
		public const int EmptyCell = -1;
		public const int Wall1Cell = 0; // 「/」：右上から左下方向に45度の角度の壁
		public const int Wall2Cell = 1; // 「\」：右下から左上方向に45度の角度の壁
		public const int PlayerCell = 2;
		public const int EnemyEmptyCell = 3; // 空セル（敵スポーン候補）
		public const int EnemyExistCell = 4; // 敵が今いるセル

		private const int FieldXSize = 5;
		private const int FieldYSize = 5;

		/// <summary> 1グリッドの1辺の長さ </summary>
		private const float GridLength = 120f;

		// Gizmo.DrawLine()を使って、反射の軌跡を描く
		private static readonly int[,] InitialGrid = new int[FieldXSize, FieldYSize]
		{
			/*y = 4*/ { EmptyCell, EnemyEmptyCell, EnemyEmptyCell, EnemyEmptyCell, EmptyCell },
			/*y = 3*/ { EnemyEmptyCell, Wall1Cell, Wall1Cell, Wall1Cell, EnemyEmptyCell },
			/*y = 2*/ { EnemyEmptyCell, Wall1Cell, PlayerCell, Wall1Cell, EnemyEmptyCell },
			/*y = 1*/ { EnemyEmptyCell, Wall1Cell, Wall1Cell, Wall1Cell, EnemyEmptyCell },
			/*y = 0*/ { EmptyCell, EnemyEmptyCell, EnemyEmptyCell, EnemyEmptyCell, EmptyCell },
		};

		private readonly int[,] grid;

		private (int x, int y) enemyGridCoordinate = (0, 0);
		
		public GridField()
		{
			grid = InitialGrid;
		}

		private (int xIndex, int yIndex) IndexToCoordinate(int xIndex, int yIndex)
		{
			// x: (0, 1, 2, 3, 4)を(-2, -1, 0, 1, 2)に変換する
			int xCoordinate = xIndex - 2;
			// y: (4, 3, 2, 1, 0)を(-2, -1, 0, 1, 2)に変換する
			int yCoordinate = -yIndex + 2;
			return (xCoordinate, yCoordinate);
		}
		
		private (int xCoordinate, int yIndex) CoordinateToIndex(int xCoordinate, int yCoordinate)
		{
			// x: (-2, -1, 0, 1, 2)を(0, 1, 2, 3, 4)に変換する
			int xIndex = xCoordinate + 2;
			// y: (-2, -1, 0, 1, 2)を(4, 3, 2, 1, 0)に変換する
			int yIndex = -yCoordinate + 2;
			return (xIndex, yIndex);
		}
		
		/// <summary>
		/// 敵をスポーンさせる
		/// </summary>
		public (int xCoordinate, int yCoordinate) SpawnEnemy()
		{
			(int oldXIndex, int oldYIndex) = CoordinateToIndex(enemyGridCoordinate.x, enemyGridCoordinate.y);
			if (grid[oldXIndex, oldYIndex] == EnemyExistCell)
			{
				grid[oldXIndex, oldYIndex] = EnemyEmptyCell; // エネミー除去
			}
			
			// gridの中からEnemyEmptyCellなセルを抽出して、その座標を配列に記憶する
			var enemyCellIndexList = new List<(int xIndex, int yIndex)>();
			for (int xIndex = 0; xIndex < FieldXSize; xIndex++)
			{
				for (int yIndex = 0; yIndex < FieldYSize; yIndex++)
				{
					if (grid[xIndex, yIndex] == EnemyEmptyCell)
					{
						enemyCellIndexList.Add((xIndex, yIndex));
					}
				}
			}
			// enemyCellIndexListの中からランダムに1つ選ぶ
			var targetCell = enemyCellIndexList[Random.Range(0, enemyCellIndexList.Count)];
			grid[targetCell.xIndex, targetCell.yIndex] = EnemyExistCell; // エネミー設定
			
			// 前と同じ座標だったら再計算
			if (oldXIndex == targetCell.xIndex && oldYIndex == targetCell.yIndex)
			{
				Debug.LogWarning("敵のスポーン座標が前回と同じです。再計算します");
				return SpawnEnemy();
			}
			
			// 返す値はグリッド座標に変換する
			enemyGridCoordinate = IndexToCoordinate(targetCell.xIndex, targetCell.yIndex);
			return enemyGridCoordinate;
		}
		
		/// <summary>
		/// グリッド座標を指定して、セルのアンカー座標を取得する
		/// </summary>
		public Vector2 GetCellAnchoredPosition(int xCoordinate, int yCoordinate)
		{
			return new Vector2(xCoordinate * GridLength, yCoordinate * GridLength);
		}
		
		/// <summary>
		/// 範囲外かどうか
		/// </summary>
		public bool IsOutOfRange(int xCoordinate, int yCoordinate)
		{
			return xCoordinate < -2 || xCoordinate > 2 || yCoordinate < -2 || yCoordinate > 2;
		}
		
		/// <summary>
		/// グリッド座標を指定して、セルの状態を更新する
		/// </summary>
		public void SetCell(int xCoordinate, int yCoordinate, int value)
		{
			var (xIndex, yIndex) = CoordinateToIndex(xCoordinate, yCoordinate);
			grid[xIndex, yIndex] = value;
		}

		/// <summary>
		/// グリッド座標を指定して、セルの状態を取得する
		/// </summary>
		public int GetCell(int xCoordinate, int yCoordinate)
		{
			var (xIndex, yIndex) = CoordinateToIndex(xCoordinate, yCoordinate);
			return grid[xIndex, yIndex];
		}
		
		/// <summary>
		/// 壁番号を指定して、壁の種類を切り換える
		/// </summary>
		public void ToggleWallTypeByIndex(int wallIndex)
		{
			var (x, y) = GetCoordinatesByWallIndex(wallIndex);
			int cellType = GetCell(x, y);
			Debug.Assert(cellType is Wall1Cell or Wall2Cell, "壁のセルではありません");
			
			int newWallType = cellType == Wall1Cell ? Wall2Cell : Wall1Cell;
			SetCell(x, y, newWallType);
		}

		/// <summary>
		/// 壁番号を指定して、壁の種類を取得する
		/// </summary>
		public int GetWallCellByIndex(int wallIndex)
		{
			var (x, y) = GetCoordinatesByWallIndex(wallIndex);
			return GetCell(x, y);
		}

		/// <summary>
		/// 壁番号を指定して、壁の座標を取得する
		/// </summary>
		public (int x, int y) GetCoordinatesByWallIndex(int wallIndex)
		{
			return wallIndex switch
			{
				0 => (-1, -1),
				1 => (0, -1),
				2 => (1, -1),
				3 => (-1, 0),
				4 => (1, 0),
				5 => (-1, 1),
				6 => (0, 1),
				7 => (1, 1),
				_ => throw new System.ArgumentException($"{wallIndex}番目の壁はありません")
			};
		}
	}
}