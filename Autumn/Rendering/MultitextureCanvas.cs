using System.Numerics;
using SceneGL;
using SceneGL.GLHelpers;
using SceneGL.GLWrappers;
using SceneGL.Materials;
using SceneGL.Materials.Common;
using Silk.NET.OpenGL;

namespace Autumn.Rendering;

/// <summary>
/// Canvas to render onto from other passes, mainly color and extras
/// </summary>
internal static class Canvas
{
	public static ShaderSource s_vertexShader =>
		new(
			"Canvas.vert",
			ShaderType.VertexShader,
            """
            #version 330 core
            layout (location = 0) in vec2 aPos;
            layout (location = 1) in vec2 aTexCoords;

            out vec2 TexCoords;

            void main()
            {
                gl_Position = vec4(aPos.x, aPos.y, 0.0, 1.0);
                TexCoords = aTexCoords;
            }  
            """
		);
	public static ShaderSource s_fragmentShader =>
		new(
			"Canvas.frag",
			ShaderType.FragmentShader,
            """
            #version 330 core

            in vec2 TexCoords;

            uniform usampler2D FgTexture;
            uniform sampler2D BgTexture;

            out vec4 FragColor;

            const float offset = 1.0 / 350.0; 
            void main()
            {
                FragColor = texture(BgTexture, TexCoords);
                uint tx = texture(FgTexture, TexCoords).r;
                if ((tx & 3u) == 1u)
                    FragColor *= 0.6;
                vec2 offsets[9] = vec2[](
                    vec2(-offset,  offset), // top-left
                    vec2( 0.0f,    offset), // top-center
                    vec2( offset,  offset), // top-right
                    vec2(-offset,  0.0f),   // center-left
                    vec2( 0.0f,    0.0f),   // center-center
                    vec2( offset,  0.0f),   // center-right
                    vec2(-offset, -offset), // bottom-left
                    vec2( 0.0f,   -offset), // bottom-center
                    vec2( offset, -offset)  // bottom-right    
                );

                float kernel[9] = float[](
                    2, 2, 2,
                    2, -22, 2,
                    2, 2, 2
                );
                uint hasSelection = 0u;
                vec3 sampleTex[9];
                for(int i = 0; i < 9; i++)
                {
                    uint t = texture(FgTexture, TexCoords.st + offsets[i]).r;
                    sampleTex[i] = vec3(t);
                    hasSelection += (((t & 4u) == 4u && (t != 255u)) ? 1u : 0u);
                }
                if (hasSelection > 1u)
                {
                    vec3 col = vec3(0.0);
                    for(int i = 0; i < 9; i++)
                        col += sampleTex[i] * kernel[i];
                    
                    FragColor.rgb += clamp(col, vec3(0), vec3(1));
                }
            }
            """
		);
    public static ShaderProgram Program { get; } = new(s_vertexShader, s_fragmentShader);
    public static bool TryUse(
        GL gl,
        out ProgramUniformScope scope
    ) => Program.TryUse(gl, null, [_samplers], out scope, out _);


    private static uint _smpl = 0; 
    public static uint CreateTextureSamplerA(GL gl)
    {
        if (_smpl == 0)
        { 
        TextureMagFilter magFilter = TextureMagFilter.Nearest;

        TextureMinFilter minFilter = TextureMinFilter.Nearest;

        _smpl = SamplerHelper.CreateSampler2D(gl, TextureWrapMode.ClampToEdge, TextureWrapMode.ClampToEdge, magFilter, minFilter);
        }
        return _smpl;
    }
    static List<SamplerBinding> samplerBindings = new();        
    private static ShaderParams _samplers;


    internal static class CanvasRenderer
    {
        private struct Vertex
        {
            [VertexAttribute(CombinerMaterial.POSITION_LOC, 2, VertexAttribPointerType.Float, false)]
            public Vector2 Position;
            [VertexAttribute(CombinerMaterial.COLOR_LOC, 2, VertexAttribPointerType.Float, false)]
            public Vector2 UV;
        }

        private static RenderableModel? s_model;

        public static void Initialize(GL gl)
        {
            s_model = GenerateCanvas(gl);
            CreateTextureSamplerA(gl);
            samplerBindings.Add(new ("STexture", 0, 0));
            samplerBindings.Add(new ("STexture2", 0, 0));
        }
        public static RenderableModel GenerateCanvas(GL gl)
        {
            ModelBuilder<ushort, Vertex> builder = new();
            builder!.AddPlane(
                new Vertex { Position = new Vector2(1,   1)  , UV = new Vector2(1, 1) },
                new Vertex { Position = new Vector2(1,  -1)  , UV = new Vector2(1, 0) },
                new Vertex { Position = new Vector2(-1,  1)  , UV = new Vector2(0, 1) },
                new Vertex { Position = new Vector2(-1, -1)  , UV = new Vector2(0, 0) }
            );

            return builder.GetModel(gl);
        }
        public static bool HasToReset = true;
        public static void Reset(uint tx1, uint tx2)
        {
            samplerBindings[0] = new ("BgTexture", _smpl, tx1);
            samplerBindings[1] = new ("FgTexture", _smpl, tx2);
            HasToReset = false;
        }


        public static void Render(GL gl, SceneGL.GLWrappers.Framebuffer basis)
        {
            if (HasToReset) 
            {
                Reset(basis.GetColorTexture(0), basis.GetColorTexture(2));
                _samplers = ShaderParams.FromSamplers(samplerBindings.ToArray());
            }
            if (!Canvas.TryUse(gl, out ProgramUniformScope scope))
                return;

            using (scope)
            {
                gl.Disable(EnableCap.CullFace);
                s_model!.Draw(gl);
                gl.Enable(EnableCap.CullFace);
            }
        }

    }
}