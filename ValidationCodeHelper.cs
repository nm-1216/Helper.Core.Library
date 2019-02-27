/*
 * 作用：生成验证码。
 * 联系：QQ 100101392
 * 来源：https://github.com/snipen/Helper.Core.Library
 * */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text;
using System.Web;

namespace Helper.Core.Library
{
    public class ValidationCodeHelper
    {
        #region 私有属性常量
        private const string CHAR_LIST = "23456789ABCDEFGHKMNPQRSTUVWYZ";

        private Point[] _pointList;
        private Random _random;

        private int _imageWidth;
        private int _imageHeight;
        private int _fontMinSize = 15;
        private int _fontMaxSize = 20;
        private bool _isPixel = true;
        private int _bezierCount = 1;
        private int _lineCount = 1;
        private int _rotationAngle = 40;
        private double _gaussianDeviation = 0;
        private int _brightnessValue = 0;
        private bool _isBorder = true;
        private Color _borderColor = Color.FromArgb(90, 87, 46); // 边框颜色
        private Color _backgroundColor = Color.FromArgb(243, 255, 255); //图片背景颜色
        #endregion

        #region 对外公开属性
        /// <summary>
        /// 验证码字体最小值，默认值：15
        /// </summary>
        public int FontMinSize
        {
            get { return this._fontMinSize; }
            set { this._fontMinSize = value; }
        }
        /// <summary>
        /// 验证码字体最大值，默认值：20
        /// </summary>
        public int FontMaxSize
        {
            get { return this._fontMaxSize; }
            set { this._fontMaxSize = value; }
        }
        /// <summary>
        /// 是否添加噪点，默认值：true
        /// </summary>
        public bool IsPixel
        {
            get { return this._isPixel; }
            set { this._isPixel = value; }
        }
        /// <summary>
        /// 贝塞尔曲线数量，默认值：1
        /// </summary>
        public int BezierCount
        {
            get { return this._bezierCount; }
            set { this._bezierCount = value; }
        }
        /// <summary>
        /// 直线数量，默认值：1
        /// </summary>
        public int LineCount
        {
            get { return this._lineCount; }
            set { this._lineCount = value; }
        }
        /// <summary>
        /// 验证码转动角度最大值，默认值：40
        /// </summary>
        public int RotationAngle
        {
            get { return this._rotationAngle; }
            set { this._rotationAngle = value; }
        }
        /// <summary>
        /// 高斯模糊阀值，默认值：0 表示不加高斯模糊
        /// </summary>
        public double GaussianDeviation
        {
            get { return this._gaussianDeviation; }
            set { this._gaussianDeviation = value; }
        }
        /// <summary>
        /// 明暗度阀值，默认值：0 表示不做调整
        /// </summary>
        public int BrightnessValue
        {
            get { return this._brightnessValue; }
            set { this._brightnessValue = value; }
        }
        /// <summary>
        /// 是否包含边框
        /// </summary>
        public bool IsBorder
        {
            get { return this._isBorder; }
            set { this._isBorder = value; }
        }
        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color BorderColor
        {
            get { return this._borderColor; }
            set { this._borderColor = value; }
        }
        /// <summary>
        /// 背景颜色
        /// </summary>
        public Color BackgroundColor
        {
            get { return this._backgroundColor; }
            set { this._backgroundColor = value; }
        }
        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageWidth">图片宽</param>
        /// <param name="imageHeight">图片高</param>
        public ValidationCodeHelper(int imageWidth, int imageHeight)
        {
            this._imageWidth = imageWidth;
            this._imageHeight = imageHeight;

            this._random = new Random(Guid.NewGuid().GetHashCode());

        }

        #region 对外公开方法
        /// <summary>
        /// 生成图片
        /// </summary>
        /// <param name="validationCode">验证码</param>
        /// <returns></returns>
        public byte[] ToImage(string validationCode)
        {
            this._pointList = new Point[validationCode.Length + 1];

            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            //写字符串
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; ;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                graphics.Clear(this._backgroundColor);

                graphics.DrawImageUnscaled(DrawBackground(), 0, 0);
                graphics.DrawImageUnscaled(DrawRandomString(validationCode), 0, 0);
                //对图片进行高斯模糊
                if (this._gaussianDeviation > 0)
                {
                    bitmap = new Gaussian().FilterProcessImage(this._gaussianDeviation, bitmap);
                }
                //进行暗度和亮度处理
                if (this._brightnessValue != 0)
                {
                    //对图片进行调暗处理
                    bitmap = this.AdjustBrightness(bitmap, this._brightnessValue);
                }

                using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
                {
                    //将图片 保存到内存流中
                    bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Gif);
                    //将内存流 里的 数据  转成 byte 数组 返回
                    return memoryStream.ToArray();
                }
            }
        }
        /// <summary>
        /// 生成图片，同时保存到 Cookie 或者 Session 中
        /// </summary>
        /// <param name="length">验证码长度</param>
        /// <param name="tokenName">保存 Cookie 或者 Session 名称</param>
        /// <param name="isCookie">是否 Cookie 保存，默认值：true</param>
        /// <param name="httpResponse">HttpResponseBase</param>
        /// <param name="session">HttpSessionStateBase</param>
        /// <returns></returns>
        public byte[] ToImage(int length, string tokenName, bool isCookie = true, HttpResponseBase httpResponse = null, HttpSessionStateBase session = null)
        {
            string validationCode = this.GetRandomCode(length);
            if (isCookie)
            {
                CookieHelper.SetCookie(tokenName, validationCode, httpResponse);
            }
            else
            {
                if (session != null)
                {
                    session[tokenName] = validationCode;
                }
                else
                {
                    HttpContext.Current.Session[tokenName] = validationCode;
                }
            }
            return ToImage(validationCode);
        }
        /// <summary>
        /// 生成图片，保存到 Cookie 中
        /// </summary>
        /// <param name="length">验证码长度</param>
        /// <param name="tokenName">保存 Cookie 名称</param>
        /// <param name="httpResponse">HttpResponseBase</param>
        /// <returns></returns>
        public byte[] ToImage(int length, string tokenName, HttpResponseBase httpResponse)
        {
            return ToImage(length, tokenName, true, httpResponse, null);
        }
        /// <summary>
        /// 生成图片，保存到 Session 中
        /// </summary>
        /// <param name="length">验证码长度</param>
        /// <param name="tokenName">保存 Session 名称</param>
        /// <param name="session">HttpSessionStateBase</param>
        /// <returns></returns>
        public byte[] ToImage(int length, string tokenName, HttpSessionStateBase session)
        {
            return ToImage(length, tokenName, false, null, session);
        }
        /// <summary>
        /// 验证码验证
        /// </summary>
        /// <param name="validationCode">验证码</param>
        /// <param name="tokenName">保存 Cookie 或者 Session 名称</param>
        /// <param name="isCookie">是否 Cookie 保存，默认值：true</param>
        /// <param name="httpRequest">HttpRequestBase</param>
        /// <param name="session">HttpSessionStateBase</param>
        /// <returns></returns>
        public static bool Valid(string validationCode, string tokenName, bool isCookie = true, HttpRequestBase httpRequest = null, HttpSessionStateBase session = null)
        {
            if (isCookie)
            {
                return CookieHelper.GetCookie(tokenName, httpRequest).ToLower() == validationCode.ToLower();
            }
            else
            {
                if (session != null)
                {
                    if (session[tokenName] != null)
                    {
                        return session[tokenName].ToString().ToLower() == validationCode.ToLower();
                    }
                }
                else
                {
                    if (HttpContext.Current.Session[tokenName] != null)
                    {
                        return HttpContext.Current.Session[tokenName].ToString().ToLower() == validationCode.ToLower();
                    }
                }
            }
            return false;
        }
        /// <summary>
        /// 验证码验证
        /// </summary>
        /// <param name="validationCode">验证码</param>
        /// <param name="tokenName">保存 Cookie 名称</param>
        /// <param name="httpRequest">HttpRequestBase</param>
        /// <returns></returns>
        public static bool Valid(string validationCode, string tokenName, HttpRequestBase httpRequest)
        {
            return Valid(validationCode, tokenName, true, httpRequest, null);
        }
        /// <summary>
        /// 验证码验证
        /// </summary>
        /// <param name="validationCode">验证码</param>
        /// <param name="tokenName">保存 Session 名称</param>
        /// <param name="session">HttpSessionStateBase</param>
        /// <returns></returns>
        public static bool Valid(string validationCode, string tokenName, HttpSessionStateBase session)
        {
            return Valid(validationCode, tokenName, false, null, session);
        }
        #endregion

        #region 逻辑处理私有方法
        private string GetRandomCode(int length)
        {
            int seedLength = CHAR_LIST.Length;
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index < length; index++)
            {
                stringBuilder.Append(CHAR_LIST[this._random.Next(0, seedLength)]);
            }
            return stringBuilder.ToString();
        }
        /// <summary>
        /// 画背景
        /// </summary>
        /// <returns></returns>
        private Bitmap DrawBackground()
        {
            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.Clear(Color.White);

                Rectangle rectangle = new Rectangle(0, 0, this._imageWidth, this._imageHeight);
                Brush brush = new SolidBrush(this._backgroundColor);
                graphics.FillRectangle(brush, rectangle);

                //画噪点
                if (this._isPixel)
                {
                    graphics.DrawImageUnscaled(this.DrawRandomPixel(30), 0, 0);
                }

                //画曲线
                graphics.DrawImageUnscaled(this.DrawRandomBezier(this._bezierCount), 0, 0);
                //画直线
                graphics.DrawImageUnscaled(this.DrawRandomLine(this._lineCount), 0, 0);

                if (this._isBorder)
                {
                    //绘制边框
                    graphics.DrawRectangle(new Pen(this._borderColor), 0, 0, this._imageWidth - 1, this._imageHeight - 1);
                }
            }
            return bitmap;
        }
        /// <summary>
        /// 画验证码
        /// </summary>
        /// <param name="validationCode"></param>
        /// <returns></returns>
        private Bitmap DrawRandomString(string validationCode)
        {
            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            bitmap.MakeTransparent();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.PixelOffsetMode = PixelOffsetMode.Half;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

                char[] charList = validationCode.ToCharArray();//拆散字符串成单字符数组

                //设置字体显示格式
                StringFormat stringFormat = new StringFormat(StringFormatFlags.NoClip);
                stringFormat.Alignment = StringAlignment.Center;
                stringFormat.LineAlignment = StringAlignment.Center;

                FontFamily fontFamily = new FontFamily(GenericFontFamilies.Monospace);
                Point tempPoint = new Point();

                int charCount = charList.Length;

                for (int i = 0; i < charCount; i++)
                {
                    //定义字体
                    Font font = new Font(fontFamily, this._random.Next(this._fontMinSize, this._fontMaxSize), FontStyle.Bold);
                    int fontSize = Convert.ToInt32(font.Size);

                    Point point = new Point(this._random.Next((this._imageWidth / charCount) * i + 5, (this._imageWidth / charCount) * (i + 1)), this._random.Next(this._imageHeight / 5 + fontSize / 2, this._imageHeight - fontSize / 2));

                    //如果当前字符X坐标小于字体的二分之一大小
                    if (point.X < fontSize / 2) point.X = point.X + fontSize / 2;
                    //防止文字叠加
                    if (i > 0 && (point.X - tempPoint.X < (fontSize / 2 + fontSize / 2)))
                    {
                        point.X = point.X + fontSize;
                    }
                    //如果当前字符X坐标大于图片宽度，就减去字体的宽度
                    if (point.X > (this._imageWidth - fontSize / 2))
                    {
                        point.X = this._imageWidth - fontSize / 2;
                    }
                    tempPoint = point;

                    float angle = this._random.Next(-this._rotationAngle, this._rotationAngle);//转动的度数
                    graphics.TranslateTransform(point.X, point.Y);//移动光标到指定位置
                    graphics.RotateTransform(angle);

                    //设置渐变画刷  
                    Rectangle rectangle = new Rectangle(0, 1, fontSize, fontSize);
                    Color color = GetRandomDeepColor();
                    LinearGradientBrush linearGradientBrush = new LinearGradientBrush(rectangle, color, GetLightColor(color, 120), this._random.Next(180));

                    graphics.DrawString(charList[i].ToString(), font, linearGradientBrush, 1, 1, stringFormat);

                    graphics.RotateTransform(-angle);//转回去
                    graphics.TranslateTransform(-point.X, -point.Y);//移动光标到指定位置，每个字符紧凑显示，避免被软件识别

                    this._pointList[i] = point;

                    font.Dispose();
                    linearGradientBrush.Dispose();
                }
            }
            return bitmap;
        }
        /// <summary>
        /// 画随机噪点
        /// </summary>
        /// <param name="pixNum"></param>
        /// <returns></returns>
        private Bitmap DrawRandomPixel(int pixNum)
        {
            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            bitmap.MakeTransparent();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;

                //画噪点 
                for (int i = 0; i < (this._imageHeight * this._imageWidth) / pixNum; i++)
                {
                    int x = this._random.Next(bitmap.Width);
                    int y = this._random.Next(bitmap.Height);
                    bitmap.SetPixel(x, y, GetRandomDeepColor());
                    //下移坐标重新画点
                    if ((x + 1) < bitmap.Width && (y + 1) < bitmap.Height)
                    {
                        //画图片的前景噪音点
                        graphics.DrawRectangle(new Pen(Color.Silver), this._random.Next(bitmap.Width), this._random.Next(bitmap.Height), 1, 1);
                    }

                }
            }
            return bitmap;
        }
        /// <summary>
        /// 随机生成贝塞尔曲线
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        private Bitmap DrawRandomBezier(int lineNum)
        {
            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            bitmap.MakeTransparent();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                GraphicsPath graphicsPath = new GraphicsPath();
                int tempNum = this._random.Next(lineNum);

                for (int i = 0; i < (lineNum - tempNum); i++)
                {
                    Pen pen = new Pen(GetRandomDeepColor());
                    Point[] point = {
                                    new Point(this._random.Next(1, (bitmap.Width / 10)), this._random.Next(1, (bitmap.Height))),
                                    new Point(this._random.Next((bitmap.Width / 10) * 2, (bitmap.Width / 10) * 4), this._random.Next(1, (bitmap.Height))),
                                    new Point(this._random.Next((bitmap.Width / 10) * 4, (bitmap.Width / 10) * 6), this._random.Next(1, (bitmap.Height))),
                                    new Point(this._random.Next((bitmap.Width / 10) * 8, bitmap.Width), this._random.Next(1, (bitmap.Height)))
                                };

                    graphicsPath.AddBeziers(point);
                    graphics.DrawPath(pen, graphicsPath);
                    pen.Dispose();
                }
                for (int i = 0; i < tempNum; i++)
                {
                    Pen pen = new Pen(GetRandomDeepColor());
                    Point[] point = {
                            new Point(this._random.Next(1, bitmap.Width), this._random.Next(1, bitmap.Height)),
                            new Point(this._random.Next((bitmap.Width / 10) * 2, bitmap.Width), this._random.Next(1, bitmap.Height)),
                            new Point(this._random.Next((bitmap.Width / 10) * 4, bitmap.Width), this._random.Next(1, bitmap.Height)),
                            new Point(this._random.Next(1, bitmap.Width), this._random.Next(1, bitmap.Height))
                                };
                    graphicsPath.AddBeziers(point);
                    graphics.DrawPath(pen, graphicsPath);
                    pen.Dispose();
                }
            }
            return bitmap;
        }
        /// <summary>
        /// 画直线
        /// </summary>
        /// <param name="lineNum"></param>
        /// <returns></returns>
        private Bitmap DrawRandomLine(int lineNum)
        {
            Bitmap bitmap = new Bitmap(this._imageWidth, this._imageHeight);
            bitmap.MakeTransparent();

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                for (int i = 0; i < lineNum; i++)
                {
                    Pen pen = new Pen(GetRandomDeepColor());
                    Point beginPoint = new Point(this._random.Next(1, (bitmap.Width / 5) * 2), this._random.Next(bitmap.Height));
                    Point endPoint = new Point(this._random.Next((bitmap.Width / 5) * 3, bitmap.Width), this._random.Next(bitmap.Height));
                    graphics.DrawLine(pen, beginPoint, endPoint);
                    pen.Dispose();
                }
            }
            return bitmap;
        }
        /// <summary>
        /// 生成随机深颜色
        /// </summary>
        /// <returns></returns>
        private Color GetRandomDeepColor()
        {
            return Color.FromArgb(this._random.Next(160), this._random.Next(100), this._random.Next(160));
        }
        /// <summary>
        /// 获取与当前颜色值相加后的颜色
        /// </summary>
        /// <param name="color"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private Color GetLightColor(Color color, int value)
        {
            int red = color.R, green = color.G, blue = color.B;    //越大颜色越浅
            if (red + value < 255 && red + value > 0)
            {
                red = color.R + 40;
            }
            if (green + value < 255 && green + value > 0)
            {
                green = color.G + 40;
            }
            if (blue + value < 255 && blue + value > 0)
            {
                blue = color.B + 40;
            }
            return Color.FromArgb(red, green, blue);
        }
        /// <summary>
        /// 增加或減少亮度
        /// </summary>
        /// <param name="image"></param>
        /// <param name="brightness"></param>
        /// <returns></returns>
        private Bitmap AdjustBrightness(Image image, int brightness)
        {
            Bitmap bitmap = new Bitmap(image);

            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    // 取得每一个 pixel
                    Color pixel = bitmap.GetPixel(x, y);

                    // 判断如果处理过后 255 就设定为 255 如果小于则设定为 0
                    Int32 red = ((pixel.R + brightness > 255) ? 255 : pixel.R + brightness) < 0 ? 0 : ((pixel.R + brightness > 255) ? 255 : pixel.R + brightness);
                    Int32 green = ((pixel.G + brightness > 255) ? 255 : pixel.G + brightness) < 0 ? 0 : ((pixel.G + brightness > 255) ? 255 : pixel.G + brightness);
                    Int32 blue = ((pixel.B + brightness > 255) ? 255 : pixel.B + brightness) < 0 ? 0 : ((pixel.B + brightness > 255) ? 255 : pixel.B + brightness);

                    // 将改过的 rgb 写回
                    System.Drawing.Color newColor = System.Drawing.Color.FromArgb(pixel.A, red, green, blue);
                    bitmap.SetPixel(x, y, newColor);

                }
            }
            return bitmap;
        }
        #endregion
    }

    #region 逻辑处理辅助类
    /// <summary>
    /// 高斯模糊算法
    /// </summary>
    public class Gaussian
    {
        public static double[,] Calculate1DSampleKernel(double deviation, int size)
        {
            double[,] ret = new double[size, 1];
            double sum = 0;
            int half = size / 2;
            for (int i = 0; i < size; i++)
            {
                ret[i, 0] = 1 / (Math.Sqrt(2 * Math.PI) * deviation) * Math.Exp(-(i - half) * (i - half) / (2 * deviation * deviation));
                sum += ret[i, 0];
            }
            return ret;
        }
        public static double[,] Calculate1DSampleKernel(double deviation)
        {
            int size = (int)Math.Ceiling(deviation * 3) * 2 + 1;
            return Calculate1DSampleKernel(deviation, size);
        }
        public static double[,] CalculateNormalized1DSampleKernel(double deviation)
        {
            return NormalizeMatrix(Calculate1DSampleKernel(deviation));
        }
        public static double[,] NormalizeMatrix(double[,] matrix)
        {
            double[,] ret = new double[matrix.GetLength(0), matrix.GetLength(1)];
            double sum = 0;
            for (int i = 0; i < ret.GetLength(0); i++)
            {
                for (int j = 0; j < ret.GetLength(1); j++)
                    sum += matrix[i, j];
            }
            if (sum != 0)
            {
                for (int i = 0; i < ret.GetLength(0); i++)
                {
                    for (int j = 0; j < ret.GetLength(1); j++)
                        ret[i, j] = matrix[i, j] / sum;
                }
            }
            return ret;
        }
        public static double[,] GaussianConvolution(double[,] matrix, double deviation)
        {
            double[,] kernel = CalculateNormalized1DSampleKernel(deviation);
            double[,] res1 = new double[matrix.GetLength(0), matrix.GetLength(1)];
            double[,] res2 = new double[matrix.GetLength(0), matrix.GetLength(1)];
            //x-direction
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                    res1[i, j] = ProcessPoint(matrix, i, j, kernel, 0);
            }
            //y-direction
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                    res2[i, j] = ProcessPoint(res1, i, j, kernel, 1);
            }
            return res2;
        }
        private static double ProcessPoint(double[,] matrix, int x, int y, double[,] kernel, int direction)
        {
            double res = 0;
            int half = kernel.GetLength(0) / 2;
            for (int i = 0; i < kernel.GetLength(0); i++)
            {
                int cox = direction == 0 ? x + i - half : x;
                int coy = direction == 1 ? y + i - half : y;
                if (cox >= 0 && cox < matrix.GetLength(0) && coy >= 0 && coy < matrix.GetLength(1))
                {
                    res += matrix[cox, coy] * kernel[i, 0];
                }
            }
            return res;
        }
        /// <summary>
        /// 对颜色值进行灰色处理
        /// </summary>
        /// <param name="cr"></param>
        /// <returns></returns>
        private Color Grayscale(Color color)
        {
            return Color.FromArgb(color.A, (int)(color.R * .3 + color.G * .59 + color.B * 0.11),
               (int)(color.R * .3 + color.G * .59 + color.B * 0.11),
              (int)(color.R * .3 + color.G * .59 + color.B * 0.11));
        }
        /// <summary>
        /// 对图片进行高斯模糊
        /// </summary>
        /// <param name="value">模糊数值，数值越大模糊越很</param>
        /// <param name="image">一个需要处理的图片</param>
        /// <returns></returns>
        public Bitmap FilterProcessImage(double value, Bitmap image)
        {
            Bitmap bitmap = new Bitmap(image.Width, image.Height);
            Double[,] matrixR = new Double[image.Width, image.Height];
            Double[,] matrixG = new Double[image.Width, image.Height];
            Double[,] matrixB = new Double[image.Width, image.Height];
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    matrixR[i, j] = image.GetPixel(i, j).R;
                    matrixG[i, j] = image.GetPixel(i, j).G;
                    matrixB[i, j] = image.GetPixel(i, j).B;
                }
            }
            matrixR = Gaussian.GaussianConvolution(matrixR, value);
            matrixG = Gaussian.GaussianConvolution(matrixG, value);
            matrixB = Gaussian.GaussianConvolution(matrixB, value);
            for (int i = 0; i < image.Width; i++)
            {
                for (int j = 0; j < image.Height; j++)
                {
                    Int32 R = (int)Math.Min(255, matrixR[i, j]);
                    Int32 G = (int)Math.Min(255, matrixG[i, j]);
                    Int32 B = (int)Math.Min(255, matrixB[i, j]);
                    bitmap.SetPixel(i, j, Color.FromArgb(R, G, B));
                }
            }
            return bitmap;
        }
    }
    #endregion
}
