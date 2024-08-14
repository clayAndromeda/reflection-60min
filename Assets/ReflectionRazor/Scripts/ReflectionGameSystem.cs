using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;

namespace ReflectionRazor
{
	public class ReflectionGameSystem : MonoBehaviour
	{
		[SerializeField] private Canvas canvas;
		[SerializeField] private RectTransform canvasTransform;
		
		[SerializeField] private TMP_Text _scoreText;
		[SerializeField] private TMP_Text _replayText;
		
		[SerializeField] private RectTransform playerTransform;
		[SerializeField] private RectTransform[] wallTransforms;
		[SerializeField] private RectTransform cursorTransform;
		[SerializeField] private RectTransform enemyTransform;
		[SerializeField] private RectTransform dummyRect;
		[SerializeField] private TrailRenderer laserTrailRenderer;

		[SerializeField] private ParticleSystem enemyBombParticle;
		[SerializeField] private ParticleSystem playerBombParticle;

		[Header("速度パラメータ"), SerializeField] private float laser1StepSeconds = 0.2f;
		[Header("レーザー残留時間"), SerializeField] private float laserTrailTime = 0.5f;

		private static readonly float[] CursorRotations = { 270f, 180f, 0f, 90f };

		private int currentTargetWallIndex;
		private Vector2Int laserDirection = new(1, 0);
		private readonly GridField gridField = new();

		private int score = 0;
		private float countDownTime = 60f;
		private readonly ReactiveProperty<bool> isGameEnd = new(false);

		private void Awake()
		{
			laserTrailRenderer.time = laserTrailTime;
			
			// 初回の敵生成
			SpawnEnemy();
			
			Observable.EveryUpdate()
				.Subscribe(_ =>
				{
					var nearestWallIndex = GetNearestWallIndex();

					// カーソル位置を変更する
					cursorTransform.position = wallTransforms[nearestWallIndex].position;
					currentTargetWallIndex = nearestWallIndex;

					// レーザー射出方向を変更する
					if (currentTargetWallIndex == 1) // 下
					{
						laserDirection = new Vector2Int(0, -1);
						playerTransform.rotation = Quaternion.Euler(0f, 0f, CursorRotations[0]);
					}
					else if (currentTargetWallIndex == 3) // 左
					{
						laserDirection = new Vector2Int(-1, 0);
						playerTransform.rotation = Quaternion.Euler(0f, 0f, CursorRotations[1]);
					}
					else if (currentTargetWallIndex == 4) // 右 
					{
						laserDirection = new Vector2Int(1, 0);
						playerTransform.rotation = Quaternion.Euler(0f, 0f, CursorRotations[2]);
					}
					else if (currentTargetWallIndex == 6) // 上
					{
						laserDirection = new Vector2Int(0, 1);
						playerTransform.rotation = Quaternion.Euler(0f, 0f, CursorRotations[3]);
					}
				}).AddTo(this);
			
			// スコア表示
			Observable.EveryUpdate()
				.Where(_ => !isGameEnd.Value)
				.Subscribe(_ =>
				{
					countDownTime -= Time.deltaTime;
					_scoreText.text = $"Score: {score:D2} Time: {Mathf.CeilToInt(countDownTime):D2}";

					if (countDownTime < 0f)
					{
						isGameEnd.Value = true;
					}
				}).AddTo(this);
			
			// ゲーム再開処理
			Observable.EveryUpdate()
				.Where(_ => isGameEnd.Value)
				.Where(_ => Input.GetKeyDown(KeyCode.Space)) // ゲームオーバー中Spaceが押された
				.Subscribe(_ =>
				{
					// スコアとタイマーをリセット
					score = 0;
					countDownTime = 60f;
					isGameEnd.Value = false;

					playerTransform.gameObject.SetActive(true);
				}).AddTo(this);
			
			// 右クリックで壁反転
			Observable.EveryUpdate()
				.Where(_ => !isGameEnd.Value)
				.Where(_ => Input.GetMouseButtonDown(1)) // 右クリック判定
				.Subscribe(_ =>
				{
					// 今選んでいる壁を反転
					gridField.ToggleWallTypeByIndex(currentTargetWallIndex);
					int wallType = gridField.GetWallCellByIndex(currentTargetWallIndex);
					wallTransforms[currentTargetWallIndex].rotation =
						Quaternion.Euler(0f, 0f, wallType == GridField.Wall1Cell ?  45f : -45f);
				}).AddTo(this);
			
			// 左クリックでレーザー射出
			// プレイヤーに当たったらゲーム終了
			Observable.EveryUpdate()
				.Where(_ => !isGameEnd.Value)
				.Where(_ => Input.GetMouseButtonDown(0)) // 左クリック判定
				.SubscribeAwait(async (_, ct)  =>
				{
					var (hitPoint, result) = await ShootLaserAsync(ct);
					// レーザー照射中は、他の操作を受け付けなくする
					if (result == 2) // 自分に当てたのでゲームオーバー
					{
						Instantiate(playerBombParticle, hitPoint, Quaternion.identity);
						playerTransform.gameObject.SetActive(false);
						isGameEnd.Value = true;
					}
					else if (result == 1) // 敵倒した！
					{
						Instantiate(enemyBombParticle, hitPoint, Quaternion.identity);
						score++;
						SpawnEnemy();
					}
				}).AddTo(this);

			isGameEnd.Subscribe(x =>
			{
				Debug.Log($"GameEnd: {x}");
				_replayText.gameObject.SetActive(x);
			}).AddTo(this);
		}
		
		private async UniTask<(Vector3, int)> ShootLaserAsync(CancellationToken cancellationToken)
		{
			var (points, laserResult) = SampleLaser();
			
			// 初期位置に移動した後、残っている残像を消す
			laserTrailRenderer.transform.position = points[0];
			laserTrailRenderer.Clear();
			
			for (int i = 0; i < points.Count - 1; i++)
			{
				float elapsedTime = 0f;
				while (elapsedTime < laser1StepSeconds)
				{
					laserTrailRenderer.transform.position = Vector3.Lerp(points[i], points[i + 1], Mathf.Clamp01(elapsedTime / laser1StepSeconds));
					elapsedTime += Time.deltaTime;
					await UniTask.Yield(cancellationToken);
				}
				laserTrailRenderer.transform.position = points[i + 1];
			}
			
			return (points.Last(), laserResult);
		}

		private void SpawnEnemy()
		{
			(int enemyXCoordinate, int enemyYCoordinate) = gridField.SpawnEnemy();
			// エネミーの位置に敵を表示する
			var position = gridField.GetCellAnchoredPosition(enemyXCoordinate, enemyYCoordinate);
			enemyTransform.anchoredPosition = position;
		}

		private int GetNearestWallIndex()
		{
			// wallTransformsの中で、cursorPositionと最も距離が近いインデックスを取得
			Vector2 cursorPosition = Input.mousePosition;
			int nearestWallIndex = 0;
			float nearestDistance = float.MaxValue;
			for (int i = 0; i < wallTransforms.Length; i++)
			{
				Vector2 wallPosition = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, wallTransforms[i].CenterPosition());
				float distance = Vector2.Distance(cursorPosition, wallPosition);
				if (distance < nearestDistance)
				{
					nearestWallIndex = i;
					nearestDistance = distance;
				}
			}

			return nearestWallIndex;
		}

		private (List<Vector3> points, int result) SampleLaser()
		{
			Vector2Int startPosition = new Vector2Int(0, 0);
			Vector2Int currentPosition = startPosition;
			Vector2Int currentLaserVector = laserDirection;
			
			// 線の座標リスト
			List<Vector3> points = new List<Vector3>();

			int sampleCount = 0;
			while (sampleCount < 10) // TODO: 無限ループ対策で、とりあえず最大10回まで
			{
				Vector2Int newPosition = currentPosition + currentLaserVector;
				Vector2 canvasStartPoint = gridField.GetCellAnchoredPosition(currentPosition.x, currentPosition.y);
				Vector2 canvasEndPoint = gridField.GetCellAnchoredPosition(newPosition.x, newPosition.y);

				// Canvas座標 -> Screen座標 -> World座標に変換
				dummyRect.anchoredPosition = canvasStartPoint;
				Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, dummyRect.position);
				RectTransformUtility.ScreenPointToWorldPointInRectangle(playerTransform, screenPoint, canvas.worldCamera, out Vector3 worldStartPoint);
				
				dummyRect.anchoredPosition = canvasEndPoint;
				screenPoint = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, dummyRect.position);
				RectTransformUtility.ScreenPointToWorldPointInRectangle(playerTransform, screenPoint, canvas.worldCamera, out Vector3 worldEndPoint);
				
				if (points.Count == 0)
				{
					points.Add(worldStartPoint);
				}
				points.Add(worldEndPoint);

				if (gridField.IsOutOfRange(newPosition.x, newPosition.y))
				{
					return (points, 0); // 範囲外
				}
				
				if (gridField.GetCell(newPosition.x, newPosition.y) == GridField.EnemyExistCell)
				{
					return (points, 1); // 敵に当たった
				}
				
				if (gridField.GetCell(newPosition.x, newPosition.y) == GridField.PlayerCell)
				{
					return (points, 2); // プレイヤーに当たった
				}

				int newCell = gridField.GetCell(newPosition.x, newPosition.y);
				if (newCell == GridField.Wall1Cell || newCell == GridField.Wall2Cell)
				{
					// 壁に反射。次のステップの方向ベクトルを計算する
					currentLaserVector = ReflectionLogics.CalculateReflectionVector(currentLaserVector, newCell);
				}
				
				currentPosition = newPosition;

				sampleCount++;
			}

			return (points, -1); // エラー？
		}

		private void DrawGizmoLines()
		{
			var (points, _) = SampleLaser();
			
			for (int i = 0; i < points.Count - 1; i++)
			{
				Gizmos.color = Color.Lerp(Color.red, Color.blue, (float)i / points.Count);
				Gizmos.DrawLine(points[i], points[i + 1]);
			}
		}
		
		private void OnDrawGizmos()
		{
			DrawGizmoLines();
		}
	}
}