#version 440 core

out vec4 FragColor;

uniform vec3 iResolution;
uniform float iTime;
uniform int zoomPointSeed;
uniform int selectedPalette;

uniform float waveHeight;

#define fragCoord (gl_FragCoord.xy)

#define resolution (iResolution.xy)

// Math constants
#define M_PI 3.1415926535897932384626433832795
#define EPSILON 0.01

// Raymarching constants
#define MAX_STEPS 315
#define MAX_DIST 100.0
#define FOV (M_PI / 2) // In radians

const int NUM_DROPLETS = 30;
uniform vec3 dropletCenters[NUM_DROPLETS];
uniform float dropletRadii[NUM_DROPLETS];

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

	for(int i = 0; i < 6; ++i){
		w += ww * wave(uv * 0.06 , vec2(sin(iter), cos(iter)) * 10.0, 2.0 + iter * 0.08, 2.0 + iter * 3.0);
		sw += ww;
		ww = mix(ww, 0.0115, 0.4);
		iter += GOLDEN_ANGLE_RADIAN;
	}
	
	return w / sw;
}

// Higher precision waves function which is more expensive.
// Used for normal estimation.
float getwavesHI(vec2 uv){
	float w = 0.0;
	float sw = 0.0;
	float iter = 0.0;
	float ww = 1.0;
    uv += iTime * 0.5;

	for(int i = 0; i < 24; ++i){
		w += ww * wave(uv * 0.06 , vec2(sin(iter), cos(iter)) * 10.0, 2.0 + iter * 0.08, 2.0 + iter * 3.0);
		sw += ww;
		ww = mix(ww, 0.0115, 0.4);
		iter += GOLDEN_ANGLE_RADIAN;
	}
	
	return w / sw;
}

// Determine the unit vector to march along
vec3 rayDirection(in float fieldOfView, in vec2 size, in vec2 fragmentCoord) {
    vec2 xy = fragmentCoord - size / 2.0;
    float z = size.y / tan(fieldOfView / 2.0);
    return normalize(vec3(xy, -z));
}

// Random number generation function
float rand2sTimex(vec2 co){
    return fract(sin(dot(co.xy * iTime,vec2(12.9898,78.233))) * 43758.5453);
}

#define iSteps 16
#define jSteps 8

vec2 rsi(vec3 r0, vec3 rd, float sr) {
    // ray-sphere intersection that assumes
    // the sphere is centered at the origin.
    // No intersection when result.x > result.y
    float a = dot(rd, rd);
    float b = 2.0 * dot(rd, r0);
    float c = dot(r0, r0) - (sr * sr);
    float d = (b*b) - 4.0*a*c;
    if (d < 0.0) return vec2(1e5,-1e5);
    return vec2(
        (-b - sqrt(d))/(2.0*a),
        (-b + sqrt(d))/(2.0*a)
    );
}

vec3 atmosphere(vec3 r, vec3 r0, vec3 pSun, float iSun, float rPlanet, float rAtmos, vec3 kRlh, float kMie, float shRlh, float shMie, float g) {
    // Normalize the sun and view directions.
    pSun = normalize(pSun);
    r = normalize(r);

    // Calculate the step size of the primary ray.
    vec2 p = rsi(r0, r, rAtmos);
    if (p.x > p.y) return vec3(0,0,0);
    p.y = min(p.y, rsi(r0, r, rPlanet).x);
    float iStepSize = (p.y - p.x) / float(iSteps);

    // Initialize the primary ray time.
    float iTime = 0.0;

    // Initialize accumulators for Rayleigh and Mie scattering.
    vec3 totalRlh = vec3(0,0,0);
    vec3 totalMie = vec3(0,0,0);

    // Initialize optical depth accumulators for the primary ray.
    float iOdRlh = 0.0;
    float iOdMie = 0.0;

    // Calculate the Rayleigh and Mie phases.
    float mu = dot(r, pSun);
    float mumu = mu * mu;
    float gg = g * g;
    float pRlh = 3.0 / (16.0 * M_PI) * (1.0 + mumu);
    float pMie = 3.0 / (8.0 * M_PI) * ((1.0 - gg) * (mumu + 1.0)) / (pow(1.0 + gg - 2.0 * mu * g, 1.5) * (2.0 + gg));

    // Sample the primary ray.
    for (int i = 0; i < iSteps; i++) {

        // Calculate the primary ray sample position.
        vec3 iPos = r0 + r * (iTime + iStepSize * 0.5);

        // Calculate the height of the sample.
        float iHeight = length(iPos) - rPlanet;

        // Calculate the optical depth of the Rayleigh and Mie scattering for this step.
        float odStepRlh = exp(-iHeight / shRlh) * iStepSize;
        float odStepMie = exp(-iHeight / shMie) * iStepSize;

        // Accumulate optical depth.
        iOdRlh += odStepRlh;
        iOdMie += odStepMie;

        // Calculate the step size of the secondary ray.
        float jStepSize = rsi(iPos, pSun, rAtmos).y / float(jSteps);

        // Initialize the secondary ray time.
        float jTime = 0.0;

        // Initialize optical depth accumulators for the secondary ray.
        float jOdRlh = 0.0;
        float jOdMie = 0.0;

        // Sample the secondary ray.
        for (int j = 0; j < jSteps; j++) {

            // Calculate the secondary ray sample position.
            vec3 jPos = iPos + pSun * (jTime + jStepSize * 0.5);

            // Calculate the height of the sample.
            float jHeight = length(jPos) - rPlanet;

            // Accumulate the optical depth.
            jOdRlh += exp(-jHeight / shRlh) * jStepSize;
            jOdMie += exp(-jHeight / shMie) * jStepSize;

            // Increment the secondary ray time.
            jTime += jStepSize;
        }

        // Calculate attenuation.
        vec3 attn = exp(-(kMie * (iOdMie + jOdMie) + kRlh * (iOdRlh + jOdRlh)));

        // Accumulate scattering.
        totalRlh += odStepRlh * attn;
        totalMie += odStepMie * attn;

        // Increment the primary ray time.
        iTime += iStepSize;

    }

    // Calculate and return the final color.
    return iSun * (pRlh * kRlh * totalRlh + pMie * kMie * totalMie);
}

float sun(vec3 ray, vec3 sunPos) {
 	vec3 sd = -normalize(sunPos);
    return pow(max(0.0, dot(ray, sd)), 528.0) * 110.0;
}

// Signed-distance function of a sphere
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float sphereSDF(in vec3 pos, in float radius, in vec3 center) {
    return length(pos + center) - radius;
}

float rainSDF(in vec3 pos, in float radius, in vec3 center) {
	return sphereSDF(pos, radius, center);
}

// Signed-distance function of a plane
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float planeSDF(in vec3 pos, in vec4 normal) {
    return dot(pos, normal.xyz) + normal.w;
}

// Displace plane SDF using waves height function
float wavesSDF(in vec3 pos) {
	float planeDist = planeSDF(pos, vec4(0.0, 1.0, 0.0, 1.0));
	float waveHeight = getwaves(pos.xz) * waveHeight - waveHeight*0.50;
	return planeDist + waveHeight;
}
float wavesSDFHI(in vec3 pos) {
	float planeDist = planeSDF(pos, vec4(0.0, 1.0, 0.0, 1.0));
	float waveHeight = getwavesHI(pos.xz) * waveHeight - waveHeight*0.50;
	return planeDist + waveHeight;
}

// Smooth union function to get that metaballs effect
// Found here: https://iquilezles.org/www/articles/distfunctions/distfunctions.htm
float smoothUnion(float d1, float d2, float k) {
    float h = clamp( 0.5 + 0.5*(d2-d1)/k, 0.0, 1.0 );
    return mix( d2, d1, h ) - k*h*(1.0-h); 
}

// Signed distance function of the entire scene to raymarch.
// All of the primitives are smooth unioned together to give that metaball look
float sceneSDF(in vec3 pos) {
	float blendingFactor = 7.0;

	float dist = wavesSDF(pos);
	for (int i = 0; i < NUM_DROPLETS; ++i) {
		dist = smoothUnion(dist, rainSDF(pos, dropletRadii[i], dropletCenters[i]), blendingFactor);
	}

	return dist;
}
float sceneSDFHI(in vec3 pos) {
	float blendingFactor = 7.0;

	float dist = wavesSDFHI(pos);
	for (int i = 0; i < NUM_DROPLETS; ++i) {
		dist = smoothUnion(dist, rainSDF(pos, dropletRadii[i], dropletCenters[i]), blendingFactor);
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
        float d = sceneSDF(pos);
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

// Estimate the normal at a point using small increments along the scene SDF.
// Allows us to avoid using calculus.
vec3 estimateNormal(in vec3 p) {
    return normalize(vec3(
            sceneSDFHI(vec3(p.x + EPSILON, p.y, p.z)) - sceneSDFHI(vec3(p.x - EPSILON, p.y, p.z)),
            sceneSDFHI(vec3(p.x, p.y + EPSILON, p.z)) - sceneSDFHI(vec3(p.x, p.y - EPSILON, p.z)),
            sceneSDFHI(vec3(p.x, p.y, p.z  + EPSILON)) - sceneSDFHI(vec3(p.x, p.y, p.z - EPSILON))
        ));
}

void main() {
	vec3 orig = vec3(0.0, 4.0, 0.0);
	vec3 ray = rayDirection(FOV, iResolution.xy, fragCoord);

	vec3 C;
	float slowedTime = iTime * 0.25;
	vec3 sunPos = normalize(vec3(0.0, 0.25, 1.0));  
	
	float dist = raymarch(orig, ray);
	if (dist == -1.0) {
		vec3 atm = atmosphere(ray, orig + vec3(0,6372e3,0), sunPos, 22.0, 6371e3, 6471e3, vec3(5.5e-6, 13.0e-6, 22.4e-6), 21e-6, 8e3, 1.2e3, 0.758);
		C = atm * 2.0 + sun(ray, sunPos);
	} else {
		vec3 normal = estimateNormal(orig + ray * dist);
		normal = mix(vec3(0.0, 1.0, 0.0), normal, 1.0 / (dist * dist * 0.01 + 1.0));
		
		vec3 reflection = reflect(ray, normal);
		float fresnel = (0.04 + (1.0-0.04)*(pow(1.0 - max(0.0, dot(-normal, ray)), 5.0)));
		
		vec3 atm = atmosphere(reflection, orig + (ray * dist) + vec3(0,6372e3,0), sunPos, 22.0, 6371e3, 6471e3, vec3(5.5e-6, 13.0e-6, 22.4e-6), 21e-6, 8e3, 1.2e3, 0.758);
		C = fresnel * atm * 2.0 + fresnel * sun(reflection, sunPos);
	}
	
    // Tonemapping
    C = normalize(C) * sqrt(length(C));
    
    
	FragColor = vec4(C,1.0);
}