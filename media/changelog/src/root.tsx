import { Composition, Still } from "remotion";
import { Film, Poster } from "./story";

export const Root = () => (
	<>
		<Composition
			id="ClipwellFilmLight"
			component={Film}
			durationInFrames={360}
			fps={30}
			width={1920}
			height={1080}
			defaultProps={{ theme: "light" as const }}
		/>
		<Composition
			id="ClipwellFilmDark"
			component={Film}
			durationInFrames={360}
			fps={30}
			width={1920}
			height={1080}
			defaultProps={{ theme: "dark" as const }}
		/>
		<Still
			id="ClipwellPosterLight"
			component={Poster}
			width={1920}
			height={1080}
			defaultProps={{ theme: "light" as const }}
		/>
		<Still
			id="ClipwellPosterDark"
			component={Poster}
			width={1920}
			height={1080}
			defaultProps={{ theme: "dark" as const }}
		/>
	</>
);
