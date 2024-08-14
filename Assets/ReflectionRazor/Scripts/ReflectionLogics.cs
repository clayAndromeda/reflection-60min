using UnityEngine;

namespace ReflectionRazor
{
	public static class ReflectionLogics
	{
		/// <summary>
		/// 壁に当たったレーザーの反射ベクトルを計算する
		/// </summary>
		public static Vector2Int CalculateReflectionVector(Vector2Int startVec, int cellType)
		{
			// (-1, 0), (1, 0), (0, -1), (0, 1)の方向にレーザーを発射する
			// CellType=0の時は、右上から左下方向に45度の角度の壁を表す
			// CellType=1の時は、右下から左上方向に45度の角度の壁を表す
			// 方向ベクトルが(-1, 0)のレーザーがCellType=0に反射すると(0, -1)に、CellType=1に反射すると(0, 1)になる
			// 方向ベクトルが(1, 0)のレーザーがCellType=0に反射すると(0, 1)に、CellType=1に反射すると(-1, 0)になる
			// 方向ベクトルが(0, -1)のレーザーがCellType=0に反射すると(-1, 0)に、CellType=1に反射すると(1, 0)になる
			// 方向ベクトルが(0, 1)のレーザーがCellType=0に反射すると(1, 0)に、CellType=1に反射すると(-1, 0)になる
			// コレを数式で表すと……
			// CellType=0の時、(x, y) -> (y, x)
			// CellType=1の時、(x, y) -> (-y, -x)

			Debug.Assert(startVec.x is -1 or 0 or 1 &&
			             startVec.y is -1 or 0 or 1 &&
			             startVec.magnitude is 1,
				"startVec must be (-1, 0), (1, 0), (0, -1), (0, 1)");
			
			return cellType switch
			{
				0 => new Vector2Int(startVec.y, startVec.x),
				1 => new Vector2Int(-startVec.y, -startVec.x),
				_ => throw new System.ArgumentException("CellType must be 0 or 1")
			};
		}
	}
}