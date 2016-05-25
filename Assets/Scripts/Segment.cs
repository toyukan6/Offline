using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnvironmentMaker {
    public class Segment : MonoBehaviour {

        private Vector2? startPos;
        private GameObject[] imageObjects;
        private Image[] images;
        private Texture2D[] firstImages;
        private Texture2D[] showImages;
        private int[][,] labels;
        private List<bool> labelButtons;
        private float[] offsetY;
        private List<Vector2>[] starts;
        private List<Vector2>[] ends;

        // Use this for initialization
        void Start() {
            imageObjects = GameObject.FindGameObjectsWithTag("Image");
            firstImages = new Texture2D[imageObjects.Length];
            images = new Image[imageObjects.Length];
            for (int i = 0; i < images.Length; i++) {
                images[i] = imageObjects[i].GetComponent<Image>();
            }
            for (int i = 0; i < firstImages.Length; i++) {
                var tex = images[i].mainTexture as Texture2D;
                firstImages[i] = new Texture2D(tex.width, tex.height);
                firstImages[i].SetPixels(tex.GetPixels());
                firstImages[i].Apply();
            }
            showImages = new Texture2D[imageObjects.Length];
            for (int i = 0; i < showImages.Length; i++) {
                var tex = images[i].mainTexture as Texture2D;
                showImages[i] = new Texture2D(tex.width, tex.height);
                showImages[i].SetPixels(tex.GetPixels());
                showImages[i].Apply();
            }
            labels = new int[imageObjects.Length][,];
            for (int i = 0; i < labels.Length; i++) {
                labels[i] = new int[showImages[i].height, showImages[i].width];
                for (int j = 0; j < labels[i].GetLength(0); j++) {
                    for (int k = 0; k < labels[i].GetLength(1); k++) {
                        labels[i][j, k] = 0;
                    }
                }
            }
            labelButtons = new List<bool>();
            labelButtons.Add(false);
            labelButtons.Add(false);
            for (int i = 0; i < imageObjects.Length; i++) {
                var texture = (Texture2D)images[i].mainTexture;
                imageObjects[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, texture.width);
                imageObjects[i].GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, texture.height);
                var colors = texture.GetPixels();
                var newTexture = new Texture2D(texture.width, texture.height);
                newTexture.SetPixels(colors);
                newTexture.Apply();
                var textureCenter = new Vector2(texture.width, texture.height) * 0.5f;
                images[i].sprite = Sprite.Create(newTexture, new UnityEngine.Rect(0, 0, newTexture.width, newTexture.height), textureCenter);
            }
            offsetY = new float[imageObjects.Length];
            float sum = 0;
            for (int i = 0; i < offsetY.Length; i++) {
                offsetY[i] = sum;
                sum += imageObjects[i].GetComponent<RectTransform>().sizeDelta.y + 10;
            }
            ChangeImagePosX();
            ChangeImagePosY();
            starts = new List<Vector2>[imageObjects.Length];
            for (int i = 0; i < starts.Length; i++) {
                starts[i] = new List<Vector2>();
                starts[i].Add(Vector2.zero);
                starts[i].Add(Vector2.zero);
            }
            ends = new List<Vector2>[imageObjects.Length];
            for (int i = 0; i < ends.Length; i++) {
                ends[i] = new List<Vector2>();
                ends[i].Add(Vector2.zero);
                ends[i].Add(Vector2.zero);
            }
            for (int i = 0; i < firstImages.Length; i++) {
                AreaExpantion(i);
            }
        }

        private void AreaExpantion(int index) {
            var queue = new Queue<Vector>();
            var image = firstImages[index];
            for (int j = 0; j < image.width; j++) {
                for (int i = 0; i < image.height; i++) {
                    var d = image.GetPixel(j, i);
                    if (d.Length() > 1.0 / 1000) {
                        queue.Enqueue(new Vector(j, i));
                        break;
                    }
                }
            }
            while (queue.Count > 0) {
                var q = queue.Dequeue();
                if (labels[index][q.Y, q.X] == 0) {
                    labels[index][q.Y, q.X] = 1;
                    var d = image.GetPixel(q.X, q.Y);
                    var candidacy = new List<Vector>();
                    if (q.X > 0) candidacy.Add(new Vector(q.X - 1, q.Y));
                    if (q.X <image.width - 1) candidacy.Add(new Vector(q.X + 1, q.Y));
                    if (q.Y > 0) candidacy.Add(new Vector(q.X, q.Y - 1));
                    if (q.Y < image.height - 1) candidacy.Add(new Vector(q.X, q.Y + 1));
                    if (q.X > 0 && q.Y > 0) candidacy.Add(new Vector(q.X - 1, q.Y - 1));
                    if (q.X > 0 && q.Y < image.height - 1) candidacy.Add(new Vector(q.X - 1, q.Y + 1));
                    if (q.X < image.width - 1 && q.Y > 0) candidacy.Add(new Vector(q.X + 1, q.Y - 1));
                    if (q.X < image.width - 1 && q.Y < image.height - 1) candidacy.Add(new Vector(q.X + 1, q.Y + 1));
                    foreach (var c in candidacy) {
                        var d2 = image.GetPixel(c.X, c.Y);
                        var l = (d - d2).Length();
                        double[] thresholds = new double[] { 18, 23 };
                        if (l < 1.0 / thresholds[index]) {
                            queue.Enqueue(c);
                        }
                    }
                }
            }
        }

        public void ChangeImagePosX() {
            Slider slider = GameObject.Find("ImagePosX").GetComponent<Slider>();
            for (int i = 0; i < imageObjects.Length; i++) {
                imageObjects[i].transform.position = new Vector3(slider.value, imageObjects[i].transform.position.y);
            }
        }

        public void ChangeImagePosY() {
            Slider slider = GameObject.Find("ImagePosY").GetComponent<Slider>();
            imageObjects[0].transform.position = new Vector3(imageObjects[0].transform.position.x, slider.value);
            for (int i = 1; i < imageObjects.Length; i++) {
                imageObjects[i].transform.position = new Vector3(imageObjects[i].transform.position.x, slider.value + offsetY[i]);
            }
        }

        // Update is called once per frame
        void Update() {
            var m_pos = Input.mousePosition;
            for (int i = 0; i < labelButtons.Count; i++) {
                if (m_pos.x > 0 && m_pos.y < Screen.height - 100 * i && m_pos .x < 100 && m_pos.y > Screen.height - 100 * (i + 1)) {
                    labelButtons[i] = true;
                } else {
                    labelButtons[i] = false;
                }
            }
            for (int i = 0; i < showImages.Length; i++) {
                bool changed = false;
                var pos = GetTexturePos(showImages[i], i);
                var colors = showImages[i].GetPixels();
                var newTexture = new Texture2D(showImages[i].width, showImages[i].height);
                newTexture.SetPixels(colors);
                var textureCenter = new Vector2(showImages[i].width, showImages[i].height) * 0.5f;
                if (pos.x >= 0 && pos.y >= 0 && pos.x < labels[i].GetLength(1) && pos.y < labels[i].GetLength(0)) {
                    if (Input.GetMouseButton(0)) {
                        if (Input.GetMouseButtonDown(0)) {
                            if (labels[i][(int)pos.y, (int)pos.x] > 0) {
                                labelButtons[labels[i][(int)pos.y, (int)pos.x]] = true;
                            } else {
                                startPos = pos;
                                starts[i].Add(pos);
                            }
                        }
                        ChangeColor(newTexture, i);
                    } else if (Input.GetMouseButtonUp(0)) {
                        ChangeColor(showImages[i], i);
                        colors = showImages[i].GetPixels();
                        newTexture.SetPixels(colors);
                        if (i == showImages.Length - 1)
                            startPos = null;
                        for (int j = 0; j < labelButtons.Count; j++) {
                            labelButtons[j] = false;
                        }
                    }
                    changed = true;
                }
                if (labelButtons.Any(lb => lb)) {
                    ChoosedLabel(newTexture, i);
                    changed = true;
                }
                newTexture.Apply();
                if (changed) {
                    DestroyImmediate(images[i].sprite);
                    images[i].sprite = Sprite.Create(newTexture, new UnityEngine.Rect(0, 0, newTexture.width, newTexture.height), textureCenter);
                } else {
                    DestroyImmediate(newTexture);
                }
            }
        }

        private void ChoosedLabel(Texture2D texture, int k) {
            for (int i = 0; i < labels[k].GetLength(0); i++) {
                for (int j = 0; j < labels[k].GetLength(1); j++) {
                    if (labelButtons[labels[k][i, j]]) {
                        var c = texture.GetPixel(j, i);
                        texture.SetPixel(j, i, new Color(c.r * 2, c.g, c.b, c.a));
                    }
                }
            }
        }

        private Vector2 GetTexturePos(Texture2D texture, int i) {
            var m_pos = Input.mousePosition;
            var i_pos = images[i].transform.position;
            return new Vector2(m_pos.x - i_pos.x + texture.width * 0.5f, m_pos.y - i_pos.y + texture.height * 0.5f);
        }

        public void OnButtonClick() {
            Vector2[][] s = new Vector2[starts.Length][];
            for (int i = 0; i < s.Length; i++) {
                s[i] = starts[i].ToArray();
            }
            Vector2[][] e = new Vector2[ends.Length][];
            for (int i = 0; i < e.Length; i++) {
                e[i] = ends[i].ToArray();
            }
            var manager = GameObject.Find("TextureManager");
            Texture2D[][] textures = new Texture2D[imageObjects.Length][];
            for (int i = 0; i < textures.Length; i++) {
                int labelMax = Maximize(labels[i]);
                textures[i] = new Texture2D[labelMax + 1];
                textures[i][0] = new Texture2D(showImages[i].width, showImages[i].height);
                textures[i][1] = new Texture2D(showImages[i].width, showImages[i].height);
                for (int j = 2; j < textures[i].Length; j++) {
                    textures[i][j] = new Texture2D((int)Mathf.Abs(starts[i][j].x - ends[i][j].x), (int)Mathf.Abs(starts[i][j].y - ends[i][j].y));
                }
            }
            for (int k = 0; k < textures.Length; k++) {
                for (int i = 0; i < labels[k].GetLength(0); i++) {
                    for (int j = 0; j < labels[k].GetLength(1); j++) {
                        var l = labels[k][i, j];
                        int y = j - (int)Mathf.Min(starts[k][l].y, ends[k][l].y);
                        int x = i - (int)Mathf.Min(starts[k][l].x, ends[k][l].x);
                        textures[k][l].SetPixel(y, x, firstImages[k].GetPixel(j, i));
                    }
                }
            }
            for (int i = 0; i < textures.Length; i++) {
                for (int j = 0; j < textures[i].Length; j++) {
                    textures[i][j].Apply();
                }
            }
            manager.SendMessage("SetTexture", textures);
            manager.SendMessage("SetStarts", s);
            manager.SendMessage("SetEnds", e);
            Application.LoadLevel("Second");
        }

        private void ChangeColor(Texture2D texture, int j) {
            var pos = GetTexturePos(texture, j);
            if (pos.x >= 0 && pos.y >= 0 && pos.x < texture.width && pos.y < texture.height && startPos.HasValue) {
                var minX = Mathf.Min(pos.x, startPos.Value.x);
                var minY = Mathf.Min(pos.y, startPos.Value.y);
                var maxX = Mathf.Max(pos.x, startPos.Value.x);
                var maxY = Mathf.Max(pos.y, startPos.Value.y);
                var sizeX = maxX - minX;
                var sizeY = maxY - minY;
                for (int i = 0; i < sizeX; i++) {
                    texture.SetPixel((int)minX + i, (int)minY, Color.white);
                    texture.SetPixel((int)minX + i, (int)maxY, Color.white);
                }
                for (int i = 0; i < sizeY; i++) {
                    texture.SetPixel((int)minX, (int)minY + i, Color.white);
                    texture.SetPixel((int)maxX, (int)minY + i, Color.white);
                }
                if (Input.GetMouseButtonUp(0)) {
                    var l = Maximize(labels[j]) + 1;
                    for (int i = 0; i < sizeX; i++) {
                        for (int k = 0; k < sizeY; k++) {
                            labels[j][(int)minY + k, (int)minX + i] = l;
                        }
                    }
                    ends[j].Add(pos);
                    if (labelButtons.Count < l + 1)
                        labelButtons.Add(false);
                }
            }
        }

        private void OnGUI() {
            for (int i = 0; i < labelButtons.Count; i++) {
                GUI.TextArea(new UnityEngine.Rect(0, 100 * i, 100, 100), "ラベル" + i);
            }
        }

        private T Maximize<T>(T[,] array) where T : IComparable {
            T max = array[0, 0];
            for (int i = 0; i < array.GetLength(0); i++) {
                for (int j = 0; j < array.GetLength(1); j++) {
                    if (max.CompareTo(array[i, j]) < 0) {
                        max = array[i, j];
                    }
                }
            }

            return max;
        }
    }

    static class ColorExtension {
        public static float Length(this Color color) {
            return Mathf.Sqrt(color.SqrLength());
        }

        public static float SqrLength(this Color color) {
            return color.r * color.r + color.g * color.g + color.b * color.b;
        }

        public static Vector3 ToVector3(this Color color) {
            return new Vector3(color.r, color.g, color.b);
        }
    }
    struct Vector {
        public int X { get; private set; }
        public int Y { get; private set; }
        public Vector(int x, int y) {
            this.X = x;
            this.Y = y;
        }
    }
}
