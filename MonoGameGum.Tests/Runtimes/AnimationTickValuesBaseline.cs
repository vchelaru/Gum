using System.Collections.Generic;

namespace MonoGameGum.Tests.Runtimes;

// Recorded on main before the per-tick interpolated state was reused (#7873). Regenerate only for an intended behavior change.
internal static class AnimationTickValuesBaseline
{
    public static readonly Dictionary<string, string> Values = new Dictionary<string, string>
    {
        ["Tick_ColorChannelsAlphaAndRotation"] = """
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:0 G=Int32:255 B=Int32:10 FillAlpha=Int32:100 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f0 Y=f0 W=f150 H=f150 Rot=f4.5 Alpha=Int32:242 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:13 G=Int32:242 B=Int32:11 FillAlpha=Int32:105 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f0 Y=f0 W=f150 H=f150 Rot=f11.7 Alpha=Int32:222 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:33 G=Int32:222 B=Int32:11 FillAlpha=Int32:113 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f0 Y=f0 W=f150 H=f150 Rot=f33.3 Alpha=Int32:161 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:94 G=Int32:161 B=Int32:14 FillAlpha=Int32:137 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f0 Y=f0 W=f150 H=f150 Rot=f45 Alpha=Int32:128 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:128 G=Int32:128 B=Int32:16 FillAlpha=Int32:150 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f0 Y=f0 W=f150 H=f150 Rot=f63.899998 Alpha=Int32:74 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:181 G=Int32:74 B=Int32:18 FillAlpha=Int32:171 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f0 Y=f0 W=f150 H=f150 Rot=f89.1 Alpha=Int32:3 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:252 G=Int32:3 B=Int32:21 FillAlpha=Int32:199 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:0 G=Int32:255 B=Int32:10 FillAlpha=Int32:100 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f0 Y=f0 W=f150 H=f150 Rot=f18 Alpha=Int32:204 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:51 G=Int32:204 B=Int32:12 FillAlpha=Int32:120 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f0 Y=f0 W=f150 H=f150 Rot=f81 Alpha=Int32:26 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:230 G=Int32:26 B=Int32:20 FillAlpha=Int32:190 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f0 Y=f0 W=f150 H=f150 Rot=f27.000002 Alpha=Int32:179 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:77 G=Int32:179 B=Int32:13 FillAlpha=Int32:130 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:0 G=Int32:255 B=Int32:10 FillAlpha=Int32:100 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f0 Y=f0 W=f150 H=f150 Rot=f15.3 Alpha=Int32:212 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:43 G=Int32:212 B=Int32:12 FillAlpha=Int32:117 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f0 Y=f0 W=f150 H=f150 Rot=f30.6 Alpha=Int32:168 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:87 G=Int32:168 B=Int32:14 FillAlpha=Int32:134 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f0 Y=f0 W=f150 H=f150 Rot=f45.899998 Alpha=Int32:125 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:130 G=Int32:125 B=Int32:16 FillAlpha=Int32:151 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f0 Y=f0 W=f150 H=f150 Rot=f61.2 Alpha=Int32:82 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:173 G=Int32:82 B=Int32:17 FillAlpha=Int32:168 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f0 Y=f0 W=f150 H=f150 Rot=f76.5 Alpha=Int32:38 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:217 G=Int32:38 B=Int32:19 FillAlpha=Int32:185 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f0 Y=f0 W=f150 H=f150 Rot=f1.8 Alpha=Int32:250 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:5 G=Int32:250 B=Int32:10 FillAlpha=Int32:102 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f0 Y=f0 W=f150 H=f150 Rot=f17.1 Alpha=Int32:207 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:48 G=Int32:207 B=Int32:12 FillAlpha=Int32:119 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f0 Y=f0 W=f150 H=f150 Rot=f32.4 Alpha=Int32:163 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:92 G=Int32:163 B=Int32:14 FillAlpha=Int32:136 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f0 Y=f0 W=f150 H=f150 Rot=f47.699997 Alpha=Int32:120 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:135 G=Int32:120 B=Int32:16 FillAlpha=Int32:153 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f0 Y=f0 W=f150 H=f150 Rot=f63 Alpha=Int32:77 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:179 G=Int32:77 B=Int32:18 FillAlpha=Int32:170 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f0 Y=f0 W=f150 H=f150 Rot=f78.3 Alpha=Int32:33 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:222 G=Int32:33 B=Int32:20 FillAlpha=Int32:187 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f0 Y=f0 W=f150 H=f150 Rot=f3.6 Alpha=Int32:245 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:10 G=Int32:245 B=Int32:10 FillAlpha=Int32:104 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f0 Y=f0 W=f150 H=f150 Rot=f18.9 Alpha=Int32:201 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:54 G=Int32:201 B=Int32:12 FillAlpha=Int32:121 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f0 Y=f0 W=f150 H=f150 Rot=f34.2 Alpha=Int32:158 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:97 G=Int32:158 B=Int32:14 FillAlpha=Int32:138 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: Fill.Red!Int32:0;Fill.Green!Int32:255;Fill.Blue!Int32:10;Alpha!Int32:255;Rotation!f0;Fill.Alpha!Int32:100;
state 0.05: Fill.Red!Int32:13;Fill.Green!Int32:242;Fill.Blue!Int32:11;Alpha!Int32:242;Rotation!f4.5;Fill.Alpha!Int32:105;
state 0.13: Fill.Red!Int32:33;Fill.Green!Int32:222;Fill.Blue!Int32:11;Alpha!Int32:222;Rotation!f11.7;Fill.Alpha!Int32:113;
state 0.37: Fill.Red!Int32:94;Fill.Green!Int32:161;Fill.Blue!Int32:14;Alpha!Int32:161;Rotation!f33.3;Fill.Alpha!Int32:137;
state 0.5: Fill.Red!Int32:128;Fill.Green!Int32:128;Fill.Blue!Int32:16;Alpha!Int32:128;Rotation!f45;Fill.Alpha!Int32:150;
state 0.71: Fill.Red!Int32:181;Fill.Green!Int32:74;Fill.Blue!Int32:18;Alpha!Int32:74;Rotation!f63.899998;Fill.Alpha!Int32:171;
state 0.99: Fill.Red!Int32:252;Fill.Green!Int32:3;Fill.Blue!Int32:21;Alpha!Int32:3;Rotation!f89.1;Fill.Alpha!Int32:199;
state 1: Fill.Red!Int32:0;Fill.Green!Int32:255;Fill.Blue!Int32:10;Alpha!Int32:255;Rotation!f0;Fill.Alpha!Int32:100;
state 1.2: Fill.Red!Int32:51;Fill.Green!Int32:204;Fill.Blue!Int32:12;Alpha!Int32:204;Rotation!f18;Fill.Alpha!Int32:120;
state 1.9: Fill.Red!Int32:230;Fill.Green!Int32:26;Fill.Blue!Int32:20;Alpha!Int32:26;Rotation!f81;Fill.Alpha!Int32:190;
state 0.3: Fill.Red!Int32:77;Fill.Green!Int32:179;Fill.Blue!Int32:13;Alpha!Int32:179;Rotation!f27.000002;Fill.Alpha!Int32:130;
state 0: Fill.Red!Int32:0;Fill.Green!Int32:255;Fill.Blue!Int32:10;Alpha!Int32:255;Rotation!f0;Fill.Alpha!Int32:100;

""",
        ["Tick_EveryInterpolationTypeAndEasing"] = """
Back/In: f3|f20 f0.85479474|f21.36928 f-5.2435656|f25.26185 f49.16083|f-9.464361 f97|f-40
Back/Out: f3|f20 f50.839172|f-10.535641 f105.24357|f-45.26185 f99.1452|f-41.369278 f97|f-40
Back/InOut: f3|f20 f-2.2748919|f23.366953 f50|f-10 f102.274895|f-43.36695 f97|f-40
Bounce/In: f3|f20 f6.966214|f17.468374 f25.03125|f5.9375 f84.98621|f-32.331627 f97|f-40
Bounce/Out: f3|f20 f15.013787|f12.331625 f74.96875|f-25.9375 f93.03378|f-37.468372 f97|f-40
Bounce/InOut: f3|f20 f3.7649274|f19.51175 f50|f-10 f96.23507|f-39.51175 f97|f-40
Circular/In: f3|f20 f3.797687|f19.490837 f15.593614|f11.961523 f50.65314|f-10.416897 f97|f-40
Circular/Out: f3|f20 f49.34686|f-9.583103 f84.40639|f-31.961525 f96.20232|f-39.490837 f97|f-40
Circular/InOut: f3|f20 f4.616396|f18.968258 f50|f-10 f95.383606|f-38.968258 f97|f-40
Cubic/In: f3|f20 f3.206518|f19.86818 f14.75|f12.5 f64.89928|f-19.510181 f97|f-40
Cubic/Out: f3|f20 f35.10072|f-0.48982048 f85.25|f-32.5 f96.79348|f-39.86818 f97|f-40
Cubic/InOut: f3|f20 f3.826072|f19.47272 f50|f-10 f96.17393|f-39.47272 f97|f-40
Elastic/In: f3|f20 f3.1828623|f19.88328 f1.5312481|f20.937502 f-31.875381|f42.260883 f97|f-40
Elastic/Out: f3|f20 f131.87538|f-62.26088 f98.46875|f-40.9375 f96.81714|f-39.88328 f97|f-40
Elastic/InOut: f3|f20 f2.8286762|f20.109356 f50|f-10 f97.171326|f-40.109356 f97|f-40
Exponential/In: f3|f20 f3.2260303|f19.855724 f5.9375|f18.125 f41.17586|f-4.367573 f97|f-40
Exponential/Out: f3|f20 f58.82414|f-15.632427 f94.0625|f-38.125 f96.77397|f-39.855724 f97|f-40
Exponential/InOut: f3|f20 f3.278276|f19.822376 f50|f-10 f96.721725|f-39.822376 f97|f-40
Instant/In: f3|f20 f97|f-40 f97|f-40 f97|f-40 f97|f-40
Instant/Out: f3|f20 f3|f20 f3|f20 f3|f20 f97|f-40
Instant/InOut: f3|f20 f3|f20 f3|f20 f3|f20 f97|f-40
Linear/In: f3|f20 f15.219999|f12.200001 f50|f-10 f84.78|f-32.2 f97|f-40
Linear/Out: f3|f20 f15.219999|f12.200001 f50|f-10 f84.78|f-32.2 f97|f-40
Linear/InOut: f3|f20 f15.219999|f12.200001 f50|f-10 f84.78|f-32.2 f97|f-40
Quadratic/In: f3|f20 f4.5886|f18.986 f26.5|f5 f74.1486|f-25.414001 f97|f-40
Quadratic/Out: f3|f20 f25.851398|f5.4140005 f73.5|f-25 f95.4114|f-38.986 f97|f-40
Quadratic/InOut: f3|f20 f6.1772|f17.972 f50|f-10 f93.8228|f-37.972 f97|f-40
Quartic/In: f3|f20 f3.0268474|f19.982862 f8.875|f16.25 f56.852375|f-14.373856 f97|f-40
Quartic/Out: f3|f20 f43.147625|f-5.6261425 f91.125|f-36.25 f96.97315|f-39.982864 f97|f-40
Quartic/InOut: f3|f20 f3.2147787|f19.862907 f50|f-10 f96.78522|f-39.862907 f97|f-40
Quintic/In: f3|f20 f3.0034902|f19.997772 f5.9375|f18.125 f49.851566|f-9.905256 f97|f-40
Quintic/Out: f3|f20 f50.14843|f-10.094742 f94.0625|f-38.125 f96.996506|f-39.997772 f97|f-40
Quintic/InOut: f3|f20 f3.0558424|f19.964355 f50|f-10 f96.94415|f-39.964355 f97|f-40
Sinusoidal/In: f3|f20 f4.9530535|f18.75337 f30.531963|f2.4264069 f77.937996|f-27.832764 f97|f-40
Sinusoidal/Out: f3|f20 f22.062006|f7.8327627 f69.46803|f-22.426407 f95.04694|f-38.75337 f97|f-40
Sinusoidal/InOut: f3|f20 f6.8655314|f17.532639 f50|f-10 f93.13447|f-37.53264 f97|f-40

""",
        ["Tick_InstanceTargetedVariables"] = """
at 0: X=f1 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f20 BoxVis=Boolean:True R=Int32:10 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f1.4 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f3 BoxW=f23 BoxVis=Boolean:True R=Int32:22 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f2.04 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f7.7999997 BoxW=f27.8 BoxVis=Boolean:True R=Int32:41 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f3.96 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f22.2 BoxW=f42.2 BoxVis=Boolean:True R=Int32:99 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f30 BoxW=f50 BoxVis=Boolean:True R=Int32:130 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f6.68 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f42.6 BoxW=f62.6 BoxVis=Boolean:True R=Int32:180 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f8.92 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f59.4 BoxW=f79.4 BoxVis=Boolean:True R=Int32:248 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f1 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f20 BoxVis=Boolean:True R=Int32:10 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f2.6 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f12 BoxW=f32 BoxVis=Boolean:True R=Int32:58 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f8.2 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f54 BoxW=f74 BoxVis=Boolean:True R=Int32:226 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f3.4 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f18 BoxW=f38 BoxVis=Boolean:True R=Int32:82 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f1 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f20 BoxVis=Boolean:True R=Int32:10 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f2.3600001 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f10.2 BoxW=f30.2 BoxVis=Boolean:True R=Int32:51 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f3.72 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f20.4 BoxW=f40.4 BoxVis=Boolean:True R=Int32:92 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f5.08 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f30.599998 BoxW=f50.6 BoxVis=Boolean:True R=Int32:132 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f6.44 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f40.8 BoxW=f60.8 BoxVis=Boolean:True R=Int32:173 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f7.8 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f51 BoxW=f71 BoxVis=Boolean:True R=Int32:214 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f1.16 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f1.1999999 BoxW=f21.2 BoxVis=Boolean:True R=Int32:15 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f2.52 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f11.4 BoxW=f31.4 BoxVis=Boolean:True R=Int32:56 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f3.88 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f21.6 BoxW=f41.6 BoxVis=Boolean:True R=Int32:96 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f5.24 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f31.8 BoxW=f51.8 BoxVis=Boolean:True R=Int32:137 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f6.6 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f42 BoxW=f62 BoxVis=Boolean:True R=Int32:178 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f7.96 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f52.2 BoxW=f72.2 BoxVis=Boolean:True R=Int32:219 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f1.3199999 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f2.3999999 BoxW=f22.4 BoxVis=Boolean:True R=Int32:20 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f2.6799998 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f12.599999 BoxW=f32.6 BoxVis=Boolean:True R=Int32:60 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f4.04 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f22.8 BoxW=f42.8 BoxVis=Boolean:True R=Int32:101 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: Box.X!f0;Box.Width!f20;Fill.Red!Int32:10;X!f1;
state 0.05: Box.X!f3;Box.Width!f23;Fill.Red!Int32:22;X!f1.4;
state 0.13: Box.X!f7.7999997;Box.Width!f27.8;Fill.Red!Int32:41;X!f2.04;
state 0.37: Box.X!f22.2;Box.Width!f42.2;Fill.Red!Int32:99;X!f3.96;
state 0.5: Box.X!f30;Box.Width!f50;Fill.Red!Int32:130;X!f5;
state 0.71: Box.X!f42.6;Box.Width!f62.6;Fill.Red!Int32:180;X!f6.68;
state 0.99: Box.X!f59.4;Box.Width!f79.4;Fill.Red!Int32:248;X!f8.92;
state 1: Box.X!f0;Box.Width!f20;Fill.Red!Int32:10;X!f1;
state 1.2: Box.X!f12;Box.Width!f32;Fill.Red!Int32:58;X!f2.6;
state 1.9: Box.X!f54;Box.Width!f74;Fill.Red!Int32:226;X!f8.2;
state 0.3: Box.X!f18;Box.Width!f38;Fill.Red!Int32:82;X!f3.4;
state 0: Box.X!f0;Box.Width!f20;Fill.Red!Int32:10;X!f1;

""",
        ["Tick_ManyKeyframes_MixedEasing"] = """
at 0: X=f0 Y=f5 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f23.4375 Y=f15.546875 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f54.437492 Y=f29.496872 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f99.43751 Y=f49.746876 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f96.938774 Y=f47.80612 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f85.06199 Y=f39.294426 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f51.20485 Y=f15.030144 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f49.260212 Y=f13.636486 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f40.15625 Y=f7.0078125 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f93.74999 Y=f47.187496 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f93.75 Y=f47.1875 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f0 Y=f5 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f66.9375 Y=f35.121876 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f97.75 Y=f48.9875 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f96.526276 Y=f47.510498 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f86.35 Y=f40.2175 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f97.876274 Y=f48.477997 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f45.926537 Y=f11.247353 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f40.20911 Y=f7.0104556 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f12.854013 Y=f5.6427007 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f-0.11004639 Y=f4.994498 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f43.74999 Y=f24.687494 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f89.4375 Y=f45.246872 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f99.12449 Y=f49.37255 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f95.8375 Y=f47.01688 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f88.53877 Y=f41.78612 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: X!f0;Y!f5;
state 0.05: X!f23.4375;Y!f15.546875;
state 0.13: X!f54.437492;Y!f29.496872;
state 0.37: X!f99.43751;Y!f49.746876;
state 0.5: X!f96.938774;Y!f47.80612;
state 0.71: X!f85.06199;Y!f39.294426;
state 0.99: X!f51.20485;Y!f15.030144;
state 1: X!f49.260212;Y!f13.636486;
state 1.2: X!f40.15625;Y!f7.0078125;
state 1.9: X!f93.74999;Y!f47.187496;
state 0.3: X!f93.75;Y!f47.1875;
state 0: X!f0;Y!f5;

""",
        ["Tick_TwoCategoriesPlayAsTwoTracks"] = """
at 0: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:219 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:153 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:46 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f99 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:0 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f90 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:0 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:235 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:148 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:61 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f85 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:0 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:224 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:138 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:51 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f87 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:0 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:214 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: X!f5;Alpha!Int32:255;
state 0.05: X!f5;Alpha!Int32:255;
state 0.13: X!f5;Alpha!Int32:255;
state 0.37: X!f5;Alpha!Int32:219;
state 0.5: X!f5;Alpha!Int32:153;
state 0.71: X!f5;Alpha!Int32:46;
state 0.99: X!f99;Alpha!Int32:0;
state 1: X!f5;Alpha!Int32:255;
state 1.2: X!f5;Alpha!Int32:255;
state 1.9: X!f90;Alpha!Int32:0;
state 0.3: X!f5;Alpha!Int32:255;
state 0: X!f5;Alpha!Int32:255;

""",
        ["Tick_WidthAndHeight_NonLooping"] = """
at 0: X=f0 Y=f0 W=f200 H=f50 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f0 Y=f0 W=f205 H=f55 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f0 Y=f0 W=f213 H=f63 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f0 Y=f0 W=f237 H=f87 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f0 Y=f0 W=f250 H=f100 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f0 Y=f0 W=f271 H=f121 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f0 Y=f0 W=f299 H=f149 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f0 Y=f0 W=f230 H=f80 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f0 Y=f0 W=f200 H=f50 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f0 Y=f0 W=f217 H=f67 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f0 Y=f0 W=f234 H=f84 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f0 Y=f0 W=f251 H=f101 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f0 Y=f0 W=f268 H=f118 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f0 Y=f0 W=f285 H=f135 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 6: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 7: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 8: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 9: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 10: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 11: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 12: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
tick 13: X=f0 Y=f0 W=f300 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=False
events: 
state 0: Width!f200;Height!f50;
state 0.05: Width!f205;Height!f55;
state 0.13: Width!f213;Height!f63;
state 0.37: Width!f237;Height!f87;
state 0.5: Width!f250;Height!f100;
state 0.71: Width!f271;Height!f121;
state 0.99: Width!f299;Height!f149;
state 1: Width!f300;Height!f150;
state 1.2: Width!f300;Height!f150;
state 1.9: Width!f300;Height!f150;
state 0.3: Width!f230;Height!f80;
state 0: Width!f200;Height!f50;

""",
        ["Tick_SubAnimationOnInstanceAndSelf_WithNamedEvent"] = """
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f5 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f13 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f37 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f19.999998 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f62 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f30.000002 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f17 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f34 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f21.999998 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f56 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f90 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: Ping@0.5,Ping@0.5
state 0: X!f0;
state 0.05: X!f5;
state 0.13: X!f13;
state 0.37: X!f37;
state 0.5: X!f19.999998;
state 0.71: X!f62;
state 0.99: X!f100;
state 1: X!f100;
state 1.2: X!f100;
state 1.9: X!f100;
state 0.3: X!f30.000002;
state 0: X!f0;

""",
        ["Tick_VariableOnlyInTheSecondKeyframe_InterpolatesFromZero"] = """
at 0: X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f15 Y=f4 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:5 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f23 Y=f10.4 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:12 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f47 Y=f29.6 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:33 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f60 Y=f40 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:45 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f81 Y=f56.8 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:64 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f109 Y=f79.2 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:89 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f10 Y=f79.2 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:89 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f30 Y=f16 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:18 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f100 Y=f72 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:81 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f40 Y=f24 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:27 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f10 Y=f24 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:27 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f27 Y=f13.6 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:15 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f44 Y=f27.2 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:31 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f61 Y=f40.8 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:46 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f78 Y=f54.4 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:61 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f95 Y=f68 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:77 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f12 Y=f1.5999999 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:2 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f29 Y=f15.2 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:17 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f46 Y=f28.800001 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:32 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f62.999996 Y=f42.399998 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:48 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f80 Y=f56 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:63 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f97 Y=f69.6 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:78 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f14 Y=f3.1999998 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:4 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f31 Y=f16.8 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:19 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f48 Y=f30.4 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:34 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: X!f10;
state 0.05: X!f15;Y!f4;Fill.Blue!Int32:5;
state 0.13: X!f23;Y!f10.4;Fill.Blue!Int32:12;
state 0.37: X!f47;Y!f29.6;Fill.Blue!Int32:33;
state 0.5: X!f60;Y!f40;Fill.Blue!Int32:45;
state 0.71: X!f81;Y!f56.8;Fill.Blue!Int32:64;
state 0.99: X!f109;Y!f79.2;Fill.Blue!Int32:89;
state 1: X!f10;
state 1.2: X!f30;Y!f16;Fill.Blue!Int32:18;
state 1.9: X!f100;Y!f72;Fill.Blue!Int32:81;
state 0.3: X!f40;Y!f24;Fill.Blue!Int32:27;
state 0: X!f10;

""",
        ["Tick_StringEnumAndBool_DoNotInterpolate"] = """
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:first HA=HorizontalAlignment:Left
at 0.05: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.13: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.37: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.5: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.71: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.99: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 1: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:first HA=HorizontalAlignment:Left
at 1.2: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 1.9: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0.3: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right
at 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:first HA=HorizontalAlignment:Left
tick 0: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 1: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 2: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 3: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 4: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 5: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 6: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 7: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 8: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 9: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 10: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 11: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 12: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
tick 13: X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:False R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:second HA=HorizontalAlignment:Right playing=True
events: 
state 0: Label.Text!String:first;Label.HorizontalAlignment!HorizontalAlignment:Left;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.05: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.13: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.37: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.5: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.71: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.99: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 1: Label.Text!String:first;Label.HorizontalAlignment!HorizontalAlignment:Left;Visible!Boolean:True;Box.Visible!Boolean:False;
state 1.2: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 1.9: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0.3: Label.Text!String:second;Label.HorizontalAlignment!HorizontalAlignment:Right;Visible!Boolean:True;Box.Visible!Boolean:False;
state 0: Label.Text!String:first;Label.HorizontalAlignment!HorizontalAlignment:Left;Visible!Boolean:True;Box.Visible!Boolean:False;

""",
        ["Tick_OneAnimationOnTwoElements_KeepsEachElementsOwnValues"] = """
X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:0 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f0 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:0 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:20 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f90 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:180 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f20 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:40 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f80 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:160 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f30.000002 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:60 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f70 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:140 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f40 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:80 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f60.000004 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:120 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f50 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:100 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f50 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:100 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f60.000004 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:120 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f40 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:80 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f70 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:140 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f30.000002 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:60 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f80 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:160 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f20 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:40 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f90 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:180 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:20 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left

""",
        ["Tick_ElementPath_VariablesOnlyInTheSecondKeyframeComeFromTheDefaultState"] = """
0: X=f0 W=f33 BoxX=f5 BoxY=f0 | X!f0;Box.X!f5;
0.05: X=f5 W=f38 BoxX=f7 BoxY=f0.45000002 | X!f5;Box.X!f7;Width!f38;Box.Y!f0.45000002;
0.13: X=f13 W=f46 BoxX=f10.2 BoxY=f1.17 | X!f13;Box.X!f10.2;Width!f46;Box.Y!f1.17;
0.37: X=f37 W=f70 BoxX=f19.8 BoxY=f3.33 | X!f37;Box.X!f19.8;Width!f70;Box.Y!f3.33;
0.5: X=f50 W=f83 BoxX=f25 BoxY=f4.5 | X!f50;Box.X!f25;Width!f83;Box.Y!f4.5;
0.71: X=f71 W=f104 BoxX=f33.4 BoxY=f6.39 | X!f71;Box.X!f33.4;Width!f104;Box.Y!f6.39;
0.99: X=f99 W=f132 BoxX=f44.6 BoxY=f8.91 | X!f99;Box.X!f44.6;Width!f132;Box.Y!f8.91;
1: X=f0 W=f132 BoxX=f5 BoxY=f8.91 | X!f0;Box.X!f5;
1.2: X=f20 W=f53 BoxX=f13 BoxY=f1.8000001 | X!f20;Box.X!f13;Width!f53;Box.Y!f1.8000001;
1.9: X=f90 W=f123 BoxX=f41 BoxY=f8.099999 | X!f90;Box.X!f41;Width!f123;Box.Y!f8.099999;
0.3: X=f30.000002 W=f63 BoxX=f17 BoxY=f2.7 | X!f30.000002;Box.X!f17;Width!f63;Box.Y!f2.7;
0: X=f0 W=f63 BoxX=f5 BoxY=f2.7 | X!f0;Box.X!f5;

""",
        ["Tick_FloatX_Loops"] = """
at 0: X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.05: X=f15 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.13: X=f23 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.37: X=f47 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.5: X=f60 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.71: X=f81 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.99: X=f109 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1: X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.2: X=f30 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 1.9: X=f100 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0.3: X=f40 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
at 0: X=f10 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left
tick 0: X=f27 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 1: X=f44 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 2: X=f61 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 3: X=f78 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 4: X=f95 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 5: X=f12 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 6: X=f29 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 7: X=f46 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 8: X=f62.999996 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 9: X=f80 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 10: X=f97 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 11: X=f14 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 12: X=f31 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
tick 13: X=f48 Y=f0 W=f150 H=f150 Rot=f0 Alpha=Int32:255 Vis=Boolean:True BoxX=f0 BoxW=f150 BoxVis=Boolean:True R=Int32:255 G=Int32:255 B=Int32:255 FillAlpha=Int32:255 Text=String:Hello World HA=HorizontalAlignment:Left playing=True
events: 
state 0: X!f10;
state 0.05: X!f15;
state 0.13: X!f23;
state 0.37: X!f47;
state 0.5: X!f60;
state 0.71: X!f81;
state 0.99: X!f109;
state 1: X!f10;
state 1.2: X!f30;
state 1.9: X!f100;
state 0.3: X!f40;
state 0: X!f10;

""",
    };
}
