import { Audio, Video } from "@remotion/media";
import { loadFont as loadDMSans } from "@remotion/google-fonts/DMSans";
import { loadFont as loadSpaceGrotesk } from "@remotion/google-fonts/SpaceGrotesk";
import {
	AbsoluteFill,
	Img,
	interpolate,
	Sequence,
	staticFile,
	useCurrentFrame,
} from "remotion";

type Theme = "light" | "dark";
type Palette = {
	ground: string;
	card: string;
	ink: string;
	muted: string;
	line: string;
	accent: string;
};

const palette: Record<Theme, Palette> = {
	light: {
		ground: "#f8f7f4",
		card: "#ffffff",
		ink: "#171510",
		muted: "#665f54",
		line: "#d7d0c5",
		accent: "#ae6128",
	},
	dark: {
		ground: "#0a0907",
		card: "#1b1915",
		ink: "#faf8f5",
		muted: "#c0b7a9",
		line: "#4a4137",
		accent: "#e09b61",
	},
};

const sans = loadDMSans("normal", {
	weights: ["400", "600", "700"],
	subsets: ["latin"],
}).fontFamily;
const display = loadSpaceGrotesk("normal", {
	weights: ["700"],
	subsets: ["latin"],
}).fontFamily;

function Wordmark({ color }: { color: string }) {
	return (
		<div
			style={{
				fontFamily: display,
				fontWeight: 700,
				fontSize: 32,
				color,
				letterSpacing: -1,
			}}
		>
			clipwell<span style={{ opacity: 0.5 }}>.</span>
		</div>
	);
}

function Title({
	children,
	theme,
	small = false,
}: {
	children: React.ReactNode;
	theme: Theme;
	small?: boolean;
}) {
	const p = palette[theme];
	return (
		<div
			style={{
				fontFamily: display,
				fontSize: small ? 58 : 92,
				lineHeight: 1.03,
				letterSpacing: -3.5,
				fontWeight: 700,
				color: p.ink,
			}}
		>
			{children}
		</div>
	);
}

function Pill({
	text,
	theme,
	style,
}: {
	text: string;
	theme: Theme;
	style?: React.CSSProperties;
}) {
	const p = palette[theme];
	return (
		<div
			style={{
				display: "inline-block",
				padding: "12px 20px",
				border: `2px solid ${p.line}`,
				borderRadius: 50,
				background: p.card,
				color: p.ink,
				font: `600 19px ${sans}`,
				boxShadow: "0 18px 60px #0002",
				...style,
			}}
		>
			{text}
		</div>
	);
}

function Background({
	theme,
	children,
}: {
	theme: Theme;
	children: React.ReactNode;
}) {
	const p = palette[theme];
	return (
		<AbsoluteFill
			style={{
				background: p.ground,
				color: p.ink,
				fontFamily: sans,
				overflow: "hidden",
			}}
		>
			<div
				style={{
					position: "absolute",
					width: 1000,
					height: 1000,
					right: -210,
					top: -320,
					borderRadius: "50%",
					background: p.accent,
					filter: "blur(190px)",
					opacity: theme === "light" ? 0.1 : 0.15,
				}}
			/>
			{children}
		</AbsoluteFill>
	);
}

/** The poster is a designed still, not an arbitrary first frame. */
export function Poster({ theme }: { theme: Theme }) {
	const p = palette[theme];
	return (
		<Background theme={theme}>
			<div
				style={{
					position: "absolute",
					inset: "68px 86px auto",
					display: "flex",
					alignItems: "center",
					justifyContent: "space-between",
				}}
			>
				<Wordmark color={p.ink} />
				<div
					style={{
						fontSize: 19,
						letterSpacing: 4,
						textTransform: "uppercase",
						color: p.muted,
					}}
				>
					Source update · 24 Sep 2026
				</div>
			</div>
			<div style={{ position: "absolute", top: 250, left: 105, width: 720 }}>
				<div
					style={{
						fontSize: 21,
						fontWeight: 700,
						textTransform: "uppercase",
						letterSpacing: 6,
						color: p.accent,
						marginBottom: 40,
					}}
				>
					Privacy / MCP reads
				</div>
				<Title theme={theme}>
					Sensitive
					<br />
					stays yours.
				</Title>
				<div
					style={{
						marginTop: 42,
						fontSize: 28,
						lineHeight: 1.5,
						color: p.muted,
					}}
				>
					Flagged items remain in your trusted picker and stay out of MCP agent
					reads.
				</div>
				<div style={{ marginTop: 55, display: "flex", gap: 14 }}>
					<Pill theme={theme} text="Trusted picker" />
					<Pill theme={theme} text="Filtered MCP reads" />
				</div>
			</div>
			<div
				style={{
					position: "absolute",
					right: 105,
					top: 170,
					width: 780,
					height: 720,
					overflow: "hidden",
					border: `2px solid ${p.line}`,
					borderRadius: 32,
					background: p.card,
					transform: "rotate(4deg)",
					boxShadow: "0 50px 100px #0004",
				}}
			>
				<Img
					src={staticFile(`picker-${theme}.png`)}
					style={{ width: "100%" }}
				/>
			</div>
			<div style={{ position: "absolute", right: 86, bottom: 115 }}>
				<Pill
					theme={theme}
					text="FLAGGED ITEM IN PICKER  ↖"
					style={{ borderColor: p.accent, color: p.accent }}
				/>
			</div>
			<div
				style={{
					position: "absolute",
					bottom: 65,
					left: 105,
					right: 105,
					height: 2,
					background: p.line,
				}}
			/>
		</Background>
	);
}

function AnimatedCaption({
	text,
	theme,
	at,
}: {
	text: string;
	theme: Theme;
	at: number;
}) {
	const frame = useCurrentFrame();
	const opacity = interpolate(frame, [at, at + 15], [0, 1], {
		extrapolateLeft: "clamp",
		extrapolateRight: "clamp",
	});
	const lift = interpolate(frame, [at, at + 15], [18, 0], {
		extrapolateLeft: "clamp",
		extrapolateRight: "clamp",
	});
	return (
		<div style={{ opacity, transform: `translateY(${lift}px)` }}>
			<Title theme={theme} small>
				{text}
			</Title>
		</div>
	);
}

export function Film({ theme }: { theme: Theme }) {
	const frame = useCurrentFrame();
	const p = palette[theme];
	const reveal = interpolate(frame, [0, 20], [0, 1], {
		extrapolateLeft: "clamp",
		extrapolateRight: "clamp",
	});
	const boundaryReveal = interpolate(frame, [210, 235], [0, 1], {
		extrapolateLeft: "clamp",
		extrapolateRight: "clamp",
	});
	const boundaryFocus = interpolate(frame, [235, 255], [0, 1], {
		extrapolateLeft: "clamp",
		extrapolateRight: "clamp",
	});
	return (
		<Background theme={theme}>
			<div
				style={{
					position: "absolute",
					top: 65,
					left: 85,
					right: 85,
					display: "flex",
					justifyContent: "space-between",
					alignItems: "center",
					zIndex: 5,
				}}
			>
				<Wordmark color={p.ink} />
				<span style={{ color: p.muted, fontSize: 20, letterSpacing: 3 }}>
					CLIPWELL / PRODUCT NOTES
				</span>
			</div>
			<Sequence from={0} durationInFrames={65}>
				<div
					style={{
						position: "absolute",
						left: 110,
						top: 330,
						opacity: reveal,
						transform: `translateY(${(1 - reveal) * 24}px)`,
					}}
				>
					<div
						style={{
							color: p.accent,
							fontSize: 25,
							letterSpacing: 5,
							fontWeight: 700,
							marginBottom: 26,
						}}
					>
						01 / RECALL
					</div>
					<Title theme={theme}>
						Your clipboard
						<br />
						remembers.
					</Title>
				</div>
				<Img
					src={staticFile(`picker-${theme}.png`)}
					style={{
						position: "absolute",
						width: 520,
						right: 170,
						top: 200,
						border: `2px solid ${p.line}`,
						borderRadius: 24,
						boxShadow: "0 40px 90px #0003",
					}}
				/>
			</Sequence>
			<Sequence from={65} durationInFrames={145}>
				<div style={{ position: "absolute", left: 110, top: 220, width: 680 }}>
					<div
						style={{
							color: p.accent,
							fontSize: 25,
							letterSpacing: 5,
							fontWeight: 700,
							marginBottom: 32,
						}}
					>
						02 / FIND
					</div>
					<AnimatedCaption theme={theme} text="Filter as you type." at={0} />
					<p
						style={{
							fontSize: 29,
							color: p.muted,
							lineHeight: 1.45,
							marginTop: 30,
						}}
					>
						Typed items, live counts, and the right copy in a few keystrokes.
					</p>
					<Pill
						theme={theme}
						text="↗ LIVE FILTERING"
						style={{ marginTop: 35, borderColor: p.accent, color: p.accent }}
					/>
				</div>
				<div
					style={{
						position: "absolute",
						right: 125,
						top: 180,
						height: 730,
						width: 620,
						border: `2px solid ${p.line}`,
						borderRadius: 26,
						overflow: "hidden",
						background: p.card,
						boxShadow: "0 40px 90px #0003",
					}}
				>
					<Video
						src={staticFile(`usage-${theme}.webm`)}
						loop
						durationInFrames={145}
						style={{ width: "100%", height: "100%", objectFit: "cover" }}
					/>
				</div>
			</Sequence>
			<Sequence from={210} durationInFrames={150}>
				<div style={{ position: "absolute", left: 110, top: 275, width: 760 }}>
					<div
						style={{
							color: p.accent,
							fontSize: 25,
							letterSpacing: 5,
							fontWeight: 700,
							marginBottom: 30,
						}}
					>
						03 / YOURS
					</div>
					<AnimatedCaption
						theme={theme}
						text="Your history. Your boundary."
						at={0}
					/>
					<p
						style={{
							fontSize: 29,
							color: p.muted,
							lineHeight: 1.5,
							marginTop: 30,
						}}
					>
						Sensitive items stay in the trusted picker and out of MCP agent
						reads.
					</p>
				</div>
				<div
					style={{
						position: "absolute",
						right: 120,
						top: 175,
						width: 680,
						height: 710,
						border: `2px solid ${p.line}`,
						borderRadius: 26,
						overflow: "hidden",
						background: p.card,
						boxShadow: "0 40px 90px #0003",
						opacity: boundaryReveal,
						transform: `translateY(${(1 - boundaryReveal) * 30}px)`,
					}}
				>
					<Img
						src={staticFile(`picker-${theme}.png`)}
						style={{ width: "100%" }}
					/>
					<div
						style={{
							position: "absolute",
							left: 36,
							right: 36,
							top: 379,
							height: 62,
							border: `4px solid ${p.accent}`,
							borderRadius: 15,
							boxShadow: `0 0 0 8px ${p.ground}bb`,
							opacity: boundaryFocus,
						}}
					/>
				</div>
				<div style={{ position: "absolute", right: 125, bottom: 100 }}>
					<Pill
						theme={theme}
						text="SENSITIVE IN PICKER  ↖"
						style={{ borderColor: p.accent, color: p.accent }}
					/>
				</div>
			</Sequence>
			<div
				style={{
					position: "absolute",
					left: 85,
					right: 85,
					bottom: 66,
					height: 2,
					background: p.line,
				}}
			/>
			<Audio src={staticFile("narration.wav")} />
		</Background>
	);
}
