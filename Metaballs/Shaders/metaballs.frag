#version 440 core

out vec4 FragColor;

uniform vec3 iResolution;
uniform float iTime;
uniform int zoomPointSeed;
uniform int selectedPalette;

#define fragCoord (gl_FragCoord.xy)

#define resolution (iResolution.xy)

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01

// Raymarching constants
#define MAX_STEPS 626
#define MAX_DIST 100.0
#define FOV (M_PI / 2.0) // In radians

// Derived from here https://github.com/achlubek/venginenative/blob/master/shaders/include/WaterHeight.glsl 
float wave(vec2 uv, vec2 emitter, float speed, float phase){
	float dst = distance(uv, emitter);
	return pow((0.5 + 0.5 * sin(dst * phase - iTime * speed)), 5.0);
}

// Waves height functions dervied from: https://www.shadertoy.com/view/MdXyzX
#define GOLDEN_ANGLE_RADIAN 2.39996
float getwaves(vec2 uv){
	float w = 0.0;
	float sw = 0.0;
	float iter = 0.0;
	float ww = 1.0;
    uv += iTime * 0.5;
	// it seems its absolutely fastest way for water height function that looks real
	for(int i=0;i<6;i++){
		w += ww * wave(uv * 0.06 , vec2(sin(iter), cos(iter)) * 10.0, 2.0 + iter * 0.08, 2.0 + iter * 3.0);
		sw += ww;
		ww = mix(ww, 0.0115, 0.4);
		iter += GOLDEN_ANGLE_RADIAN * 0.5;
	}
	
	return w / sw;
}

float getwavesHI(vec2 uv){
	float w = 0.0;
	float sw = 0.0;
	float iter = 0.0;
	float ww = 1.0;
    uv += iTime * 0.5;
	// it seems its absolutely fastest way for water height function that looks real
	for(int i=0;i<24;i++){
		w += ww * wave(uv * 0.06 , vec2(sin(iter), cos(iter)) * 10.0, 2.0 + iter * 0.08, 2.0 + iter * 3.0);
		sw += ww;
		ww = mix(ww, 0.0115, 0.4);
		iter += GOLDEN_ANGLE_RADIAN;
	}
	
	return w / sw;
}

float H = 0.0;
vec3 wavesNormal(vec2 pos, float e, float depth){
    vec2 ex = vec2(e, 0);
    H = getwavesHI(pos.xy) * depth;
    vec3 a = vec3(pos.x, H, pos.y);
    return normalize(cross(normalize(a-vec3(pos.x - e, getwavesHI(pos.xy - ex.xy) * depth, pos.y)), 
                           normalize(a-vec3(pos.x, getwavesHI(pos.xy + ex.yx) * depth, pos.y + e))));
}
mat3 rotmat(vec3 axis, float angle)
{
	axis = normalize(axis);
	float s = sin(angle);
	float c = cos(angle);
	float oc = 1.0 - c;
	
	return mat3(oc * axis.x * axis.x + c,           oc * axis.x * axis.y - axis.z * s,  oc * axis.z * axis.x + axis.y * s, 
	oc * axis.x * axis.y + axis.z * s,  oc * axis.y * axis.y + c,           oc * axis.y * axis.z - axis.x * s, 
	oc * axis.z * axis.x - axis.y * s,  oc * axis.y * axis.z + axis.x * s,  oc * axis.z * axis.z + c);
}

// Determine the unit vector to march along
vec3 rayDirection(in float fieldOfView, in vec2 size, in vec2 fragmentCoord) {
    vec2 xy = fragmentCoord - size / 2.0;
    float z = size.y / tan(fieldOfView / 2.0);
    return normalize(vec3(xy, -z));
}

float rand2sTimex(vec2 co){
    return fract(sin(dot(co.xy * iTime,vec2(12.9898,78.233))) * 43758.5453);
}


float intersectPlane(vec3 origin, vec3 direction, vec3 point, vec3 normal) { 
    return clamp(dot(point - origin, normal) / dot(direction, normal), -1.0, 9991999.0); 
}

vec3 extra_cheap_atmosphere(vec3 raydir, vec3 sundir){
	sundir.y = max(sundir.y, -0.07);
	float special_trick = 1.0 / (raydir.y * 1.0 + 0.1);
	float special_trick2 = 1.0 / (sundir.y * 11.0 + 1.0);
	float raysundt = pow(abs(dot(sundir, raydir)), 2.0);
	float sundt = pow(max(0.0, dot(sundir, raydir)), 8.0);
	float mymie = sundt * special_trick * 0.2;
	vec3 suncolor = mix(vec3(1.0), max(vec3(0.0), vec3(1.0) - vec3(5.5, 13.0, 22.4) / 22.4), special_trick2);
	vec3 bluesky= vec3(5.5, 13.0, 22.4) / 22.4 * suncolor;
	vec3 bluesky2 = max(vec3(0.0), bluesky - vec3(5.5, 13.0, 22.4) * 0.004 * (special_trick + -6.0 * sundir.y * sundir.y));
	bluesky2 *= special_trick * (0.24 + raysundt * 0.24);
	return bluesky2 + mymie * suncolor;
} 

vec3 getatm(vec3 ray){
 	return extra_cheap_atmosphere(ray, normalize(vec3(1.0))) * 0.5;
    
}

float sun(vec3 ray){
 	vec3 sd = normalize(vec3(1.0));   
    return pow(max(0.0, dot(ray, sd)), 528.0) * 110.0;
}

const int NUM_METABALLS = 2;

const vec3 metaballCenters[NUM_METABALLS] = {vec3(-1.0, 0.0, 4.2), vec3(1.0, 0.0, 4.2)};

// Signed-distance function of a sphere
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float sphereSDF(in vec3 pos, in float radius, in vec3 center) {
    return length(pos + center) - radius;
}

const float WATER_DEPTH = 2.1;

// Signed-distance function of a plane
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float planeSDF(in vec3 pos, in vec4 normal) {
    return dot(pos, normal.xyz) + normal.w;
}

// Displace plane SDF using waves height function
float wavesSDF(in vec3 pos) {
	float planeDist = planeSDF(pos, vec4(0.0, 1.0, 0.0, 1.0));
	float waveHeight = getwaves(pos.xz) * WATER_DEPTH - WATER_DEPTH*0.5;
	return planeDist + waveHeight;
}

// Smooth union function to get that metaballs effect
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float smoothUnion( float d1, float d2, float k ) {
    float h = clamp( 0.5 + 0.5*(d2-d1)/k, 0.0, 1.0 );
    return mix( d2, d1, h ) - k*h*(1.0-h); 
}

float metaballsScene(in vec3 pos) {
	float blendingFactor = 1.0;

	float dist = wavesSDF(pos);
	for (int i = 0; i < NUM_METABALLS; ++i) {
		dist = smoothUnion(dist, sphereSDF(pos, 1.0, metaballCenters[i]), blendingFactor);
	}

	return dist;
}

// The raymarching algorithm
// -------------------------
// March along a ray by the distance to the nearest object
// until that distance approaches zero (collision)
// or it exceeds the max steps or max distance
float raymarch(in vec3 eye, in vec3 rayDir) {
    float depth = 0.0;
    for (int i = 0; i < MAX_STEPS; ++i) {
	    vec3 pos = eye + rayDir * depth;
        float d = metaballsScene(pos);
        if (d < EPSILON) {
            return depth;
        }

		depth += d;

        if (depth >= MAX_DIST) {
            return -1.0;
        }
    }
    return -1.0;
}

void main() {
	vec3 orig = vec3(0.0, 0.0, 0.0);
	vec3 ray = rayDirection(FOV, iResolution.xy, fragCoord);

	/*if(ray.y >= -0.01){
        vec3 C = getatm(ray) * 2.0 + sun(ray);
        //tonemapping
        C = normalize(C) * sqrt(length(C));
     	FragColor = vec4( C,1.0);   
        return;
    }

	float waterDist = waterScene(orig, ray);
    vec3 waterPos = orig + ray * waterDist;

	vec3 normal = wavesNormal(waterPos.xz, 0.001, WATER_DEPTH);
	normal = mix(vec3(0.0, 1.0, 0.0), normal, 1.0 / (waterDist * waterDist * 0.01 + 1.0));

	vec3 reflection = reflect(ray, normal);
	float fresnel = (0.04 + (1.0-0.04)*(pow(1.0 - max(0.0, dot(-normal, ray)), 5.0)));
	
	vec3 C = fresnel * getatm(reflection) * 2.0 + fresnel * sun(reflection);

	// Tonemapping
	C = normalize(C) * sqrt(length(C));*/

	vec3 C;
	float dist = raymarch(orig, ray);
	if (dist == -1.0) {
		C = getatm(ray) * 2.0 + sun(ray);
	} else {
		C = vec3(0.0, 1.0, 0.0);
	}
	
    // Tonemapping
    //C = normalize(C) * sqrt(length(C));
    
    
	FragColor = vec4(C,1.0);
}