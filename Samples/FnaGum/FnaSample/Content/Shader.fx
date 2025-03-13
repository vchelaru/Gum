float4x4 ViewProj : VIEWPROJ; // World, view and projection matrix
uniform extern texture CurrentTexture;

sampler linearTextureSampler = sampler_state
{
    Texture = <CurrentTexture>;
    MipFilter = Linear;
    MinFilter = Linear;
    MagFilter = Linear;
};

sampler pointTextureSampler = sampler_state
{
    Texture = <CurrentTexture>;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

float4 ColorModifier = float4(1.0, 1.0, 1.0, 1.0); // Color for non-vertex color rendering

// Application to vertex structure
struct a2v
{
    float4 position : POSITION0;
    float4 color : COLOR0;
    float4 texCoord : TEXCOORD0;
    
};

// Vertex to pixel processing structure
struct v2p
{
    float4 position : POSITION0;
    float4 color : COLOR0;
    float4 texCoord : TEXCOORD0;
};

// VERTEX SHADER
void vs(in a2v IN, out v2p OUT)
{
    // Transforming our position from object space to screen space
    OUT.position = mul(IN.position, ViewProj);
    OUT.color = IN.color;
    OUT.texCoord = IN.texCoord;
}


float4 Linearize(float4 color)
{
    return float4(pow(color.rgb, 2.2), color.a);
}

#define SAMPLE(textureSampler, a2v) tex2D(textureSampler, a2v.texCoord.xy)
#define SAMPLE_LINEARIZE(textureSampler, a2v) Linearize(tex2D(textureSampler, a2v.texCoord.xy))

#define TECHNIQUE(name, psname) \
	technique name { pass { VertexShader = compile vs_1_1 vs(); PixelShader = compile ps_2_0 psname(); } }

float4 PremultiplyAlpha(float4 textureColor, float4 color)
{
    return textureColor *= color.a;
}

float4 Add(float4 textureColor, float4 color)
{
    textureColor.rgb += color.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 Subtract(float4 textureColor, float4 color)
{
    textureColor.rgb -= color.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 Modulate(float4 textureColor, float4 color)
{
    textureColor.rgb *= color.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 Modulate2X(float4 textureColor, float4 color)
{
    textureColor.rgb *= 2 * color.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 Modulate4X(float4 textureColor, float4 color)
{
    textureColor.rgb *= 4 * color.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 Inverse(float4 textureColor, float4 color)
{
    textureColor.rgb = 1 - textureColor.rgb;
    textureColor *= color.a;
    return textureColor;
}

float4 ColorTextureAlpha(float textureAlpha, float4 color)
{
    float4 returnColor = color;
    returnColor.rgb *= color.a;
    returnColor *= textureAlpha;
    return returnColor;
}

float4 InterpolateColor(float4 textureColor, float4 color)
{
    float4 returnColor = color;
    returnColor = (textureColor * returnColor.a) + (1 - returnColor.a) * (returnColor);
    returnColor.a = textureColor.a;
    return returnColor;
}


// PIXEL SHADERS:

// Point filtering

float4 TexturePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Add(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Subtract(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate2X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate4X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Inverse(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ColorPixelShader(a2v IN) : COLOR
{
    clip(IN.color.a - .001);
    return IN.color;
}

float4 ColorTextureAlphaPixelShader_Point(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(pointTextureSampler, IN).a, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(pointTextureSampler, IN), IN.color);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Add(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Subtract(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate2X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Modulate4X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(pointTextureSampler, IN);
    color = Inverse(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ColorPixelShader_CM(a2v IN) : COLOR
{
    clip(ColorModifier.a - .001);
    return ColorModifier;
}

float4 ColorTextureAlphaPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(pointTextureSampler, IN).a, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point_CM(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(pointTextureSampler, IN), ColorModifier);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Add(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Subtract(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate2X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate4X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Inverse(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(pointTextureSampler, IN).a, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point_LN(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(pointTextureSampler, IN), IN.color);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = PremultiplyAlpha(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Add(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Subtract(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate2X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Modulate4X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(pointTextureSampler, IN);
    color = Inverse(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(pointTextureSampler, IN).a, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Point_LN_CM(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(pointTextureSampler, IN), ColorModifier);
    clip(color.a - .001);
    return color;
}


// Linear filtering

float4 TexturePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Add(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Subtract(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate2X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate4X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Inverse(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(linearTextureSampler, IN).a, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(linearTextureSampler, IN), IN.color);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Add(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Subtract(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate2X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Modulate4X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE(linearTextureSampler, IN);
    color = Inverse(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE(linearTextureSampler, IN).a, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear_CM(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE(linearTextureSampler, IN), ColorModifier);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Add(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Subtract(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate2X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate4X(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Inverse(color, IN.color);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(linearTextureSampler, IN).a, IN.color);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear_LN(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(linearTextureSampler, IN), IN.color);
    clip(color.a - .001);
    return color;
}


float4 TexturePixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = PremultiplyAlpha(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 AddPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Add(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 SubtractPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Subtract(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ModulatePixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate2XPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate2X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 Modulate4XPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Modulate4X(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InversePixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = SAMPLE_LINEARIZE(linearTextureSampler, IN);
    color = Inverse(color, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 ColorTextureAlphaPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = ColorTextureAlpha(SAMPLE_LINEARIZE(linearTextureSampler, IN).a, ColorModifier);
    clip(color.a - .001);
    return color;
}

float4 InterpolateColorPixelShader_Linear_LN_CM(a2v IN) : COLOR
{
    float4 color = InterpolateColor(SAMPLE_LINEARIZE(linearTextureSampler, IN), ColorModifier);
    clip(color.a - .001);
    return color;
}


// TECHNIQUES:

// Point filtering
TECHNIQUE(Texture_Point, TexturePixelShader_Point);
TECHNIQUE(Add_Point, AddPixelShader_Point);
TECHNIQUE(Subtract_Point, SubtractPixelShader_Point);
TECHNIQUE(Modulate_Point, ModulatePixelShader_Point);
TECHNIQUE(Modulate2X_Point, Modulate2XPixelShader_Point);
TECHNIQUE(Modulate4X_Point, Modulate4XPixelShader_Point);
TECHNIQUE(InverseTexture_Point, InversePixelShader_Point);
TECHNIQUE(Color_Point, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Point, ColorTextureAlphaPixelShader_Point);
TECHNIQUE(InterpolateColor_Point, InterpolateColorPixelShader_Point);

// Point filtering, use color modifier
TECHNIQUE(Texture_Point_CM, TexturePixelShader_Point_CM);
TECHNIQUE(Add_Point_CM, AddPixelShader_Point_CM);
TECHNIQUE(Subtract_Point_CM, SubtractPixelShader_Point_CM);
TECHNIQUE(Modulate_Point_CM, ModulatePixelShader_Point_CM);
TECHNIQUE(Modulate2X_Point_CM, Modulate2XPixelShader_Point_CM);
TECHNIQUE(Modulate4X_Point_CM, Modulate4XPixelShader_Point_CM);
TECHNIQUE(InverseTexture_Point_CM, InversePixelShader_Point_CM);
TECHNIQUE(Color_Point_CM, ColorPixelShader_CM);
TECHNIQUE(ColorTextureAlpha_Point_CM, ColorTextureAlphaPixelShader_Point_CM);
TECHNIQUE(InterpolateColor_Point_CM, InterpolateColorPixelShader_Point_CM);

// Point filtering, linearize texture sampling
TECHNIQUE(Texture_Point_LN, TexturePixelShader_Point_LN);
TECHNIQUE(Add_Point_LN, AddPixelShader_Point_LN);
TECHNIQUE(Subtract_Point_LN, SubtractPixelShader_Point_LN);
TECHNIQUE(Modulate_Point_LN, ModulatePixelShader_Point_LN);
TECHNIQUE(Modulate2X_Point_LN, Modulate2XPixelShader_Point_LN);
TECHNIQUE(Modulate4X_Point_LN, Modulate4XPixelShader_Point_LN);
TECHNIQUE(InverseTexture_Point_LN, InversePixelShader_Point_LN);
TECHNIQUE(Color_Point_LN, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Point_LN, ColorTextureAlphaPixelShader_Point_LN);
TECHNIQUE(InterpolateColor_Point_LN, InterpolateColorPixelShader_Point_LN);

// Point filtering, linearize texture sampling, use color modifier
TECHNIQUE(Texture_Point_LN_CM, TexturePixelShader_Point_LN_CM);
TECHNIQUE(Add_Point_LN_CM, AddPixelShader_Point_LN_CM);
TECHNIQUE(Subtract_Point_LN_CM, SubtractPixelShader_Point_LN_CM);
TECHNIQUE(Modulate_Point_LN_CM, ModulatePixelShader_Point_LN_CM);
TECHNIQUE(Modulate2X_Point_LN_CM, Modulate2XPixelShader_Point_LN_CM);
TECHNIQUE(Modulate4X_Point_LN_CM, Modulate4XPixelShader_Point_LN_CM);
TECHNIQUE(InverseTexture_Point_LN_CM, InversePixelShader_Point_LN_CM);
TECHNIQUE(Color_Point_LN_CM, ColorPixelShader_CM);
TECHNIQUE(ColorTextureAlpha_Point_LN_CM, ColorTextureAlphaPixelShader_Point_LN_CM);
TECHNIQUE(InterpolateColor_Point_LN_CM, InterpolateColorPixelShader_Point_LN_CM);

// Linear filtering
TECHNIQUE(Texture_Linear, TexturePixelShader_Linear);
TECHNIQUE(Add_Linear, AddPixelShader_Linear);
TECHNIQUE(Subtract_Linear, SubtractPixelShader_Linear);
TECHNIQUE(Modulate_Linear, ModulatePixelShader_Linear);
TECHNIQUE(Modulate2X_Linear, Modulate2XPixelShader_Linear);
TECHNIQUE(Modulate4X_Linear, Modulate4XPixelShader_Linear);
TECHNIQUE(InverseTexture_Linear, InversePixelShader_Linear);
TECHNIQUE(Color_Linear, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Linear, ColorTextureAlphaPixelShader_Linear);
TECHNIQUE(InterpolateColor_Linear, InterpolateColorPixelShader_Linear);

// Linear filtering, use color modifier
TECHNIQUE(Texture_Linear_CM, TexturePixelShader_Linear_CM);
TECHNIQUE(Add_Linear_CM, AddPixelShader_Linear_CM);
TECHNIQUE(Subtract_Linear_CM, SubtractPixelShader_Linear_CM);
TECHNIQUE(Modulate_Linear_CM, ModulatePixelShader_Linear_CM);
TECHNIQUE(Modulate2X_Linear_CM, Modulate2XPixelShader_Linear_CM);
TECHNIQUE(Modulate4X_Linear_CM, Modulate4XPixelShader_Linear_CM);
TECHNIQUE(InverseTexture_Linear_CM, InversePixelShader_Linear_CM);
TECHNIQUE(Color_Linear_CM, ColorPixelShader_CM);
TECHNIQUE(ColorTextureAlpha_Linear_CM, ColorTextureAlphaPixelShader_Linear_CM);
TECHNIQUE(InterpolateColor_Linear_CM, InterpolateColorPixelShader_Linear_CM);

// Linear filtering, linearize texture sampling
TECHNIQUE(Texture_Linear_LN, TexturePixelShader_Linear_LN);
TECHNIQUE(Add_Linear_LN, AddPixelShader_Linear_LN);
TECHNIQUE(Subtract_Linear_LN, SubtractPixelShader_Linear_LN);
TECHNIQUE(Modulate_Linear_LN, ModulatePixelShader_Linear_LN);
TECHNIQUE(Modulate2X_Linear_LN, Modulate2XPixelShader_Linear_LN);
TECHNIQUE(Modulate4X_Linear_LN, Modulate4XPixelShader_Linear_LN);
TECHNIQUE(InverseTexture_Linear_LN, InversePixelShader_Linear_LN);
TECHNIQUE(Color_Linear_LN, ColorPixelShader);
TECHNIQUE(ColorTextureAlpha_Linear_LN, ColorTextureAlphaPixelShader_Linear_LN);
TECHNIQUE(InterpolateColor_Linear_LN, InterpolateColorPixelShader_Linear_LN);

// Linear filtering, linearize texture sampling, use color modifier
TECHNIQUE(Texture_Linear_LN_CM, TexturePixelShader_Linear_LN_CM);
TECHNIQUE(Add_Linear_LN_CM, AddPixelShader_Linear_LN_CM);
TECHNIQUE(Subtract_Linear_LN_CM, SubtractPixelShader_Linear_LN_CM);
TECHNIQUE(Modulate_Linear_LN_CM, ModulatePixelShader_Linear_LN_CM);
TECHNIQUE(Modulate2X_Linear_LN_CM, Modulate2XPixelShader_Linear_LN_CM);
TECHNIQUE(Modulate4X_Linear_LN_CM, Modulate4XPixelShader_Linear_LN_CM);
TECHNIQUE(InverseTexture_Linear_LN_CM, InversePixelShader_Linear_LN_CM);
TECHNIQUE(Color_Linear_LN_CM, ColorPixelShader_CM);
TECHNIQUE(ColorTextureAlpha_Linear_LN_CM, ColorTextureAlphaPixelShader_Linear_LN_CM);
TECHNIQUE(InterpolateColor_Linear_LN_CM, InterpolateColorPixelShader_Linear_LN_CM);