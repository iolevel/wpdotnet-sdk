<?php
/**
 * The base configuration for WordPress
 *
 * This file contains the following configurations:
 *
 * * MySQL settings
 * * Secret keys
 * * Database table prefix
 * * ABSPATH
 *
 * @link https://codex.wordpress.org/Editing_wp-config.php
 *
 * @package WordPress
 */

// Following settings have been moved to the SDK
// so they are read from the app's configuration.
// See \PeachPied.WordPress.AspNetCore\RequestDelegateExtension.cs

// ** MySQL settings - You can get this info from your web host ** //
/** The name of the database for WordPress */
//define('DB_NAME', 'wordpress');

/** MySQL database username */
//define('DB_USER', 'root');

/** MySQL database password */
//define('DB_PASSWORD', 'password');

/** MySQL hostname */
//define('DB_HOST', 'localhost');

/** Database Charset to use in creating database tables. */
//define('DB_CHARSET', 'utf8');

/** The Database Collate type. Don't change this if in doubt. */
//define('DB_COLLATE', '');

/**#@+
 * Authentication Unique Keys and Salts.
 *
 * Change these to different unique phrases!
 * You can generate these using the {@link https://api.wordpress.org/secret-key/1.1/salt/ WordPress.org secret-key service}
 * You can change these at any point in time to invalidate all existing cookies. This will force all users to have to log in again.
 *
 * @since 2.6.0
 */
//define('AUTH_KEY',         '*Sr748b66z3R+(v%1z;|SCtBZz/cEvo1)mo|F&EO>5a^1aF6@C9^KIzG&MD?=Zmt');
//define('SECURE_AUTH_KEY',  'P]-!;,$G96Gf`8pO-1e;t%Y1hYfU{}lRdhgl#h./C`_gxJsd^`3[$yoz!pe4bX(U');
//define('LOGGED_IN_KEY',    'E$0Y`&8,IgAME5<OTD:*]x}$wEhEemY|2PVzQ!!96:F&0S{gu|S|TZ!} ^-l}xgh');
//define('NONCE_KEY',        '0)ET<zQ RlA$Gb5R*>UO]zKpgF-CxT?J0u8<m?;HhpAm!aY @qWTNI{A]>$Jow#>');
//define('AUTH_SALT',        '!|gQ:L<;]+F:mt<wV)]n &,7iv{D5dG+kLi<S$}Vp-*@Ev.+-P4p|lRQOnh]2jKV');
//define('SECURE_AUTH_SALT', 'wlk)xBD7EC0|zJCs&`&oK#3<O2THx,{=He|^]+PFwVN%{m38bK.||-]@-1:4,7}f');
//define('LOGGED_IN_SALT',   'g}oD ]M2)SMa^zPx(}~6RPXP{7{!|`(IQCnY.2xQHv4HxV9f8;CoH~+]M01w/o(y');
//define('NONCE_SALT',       'SVVq/47*B)T_&aFj.tN^c9U =uI>7QS+WSuR[leI+PpDbJ_K_fu06Qyrq~5s{3=-');

/**#@-*/

/**
 * WordPress Database Table prefix.
 *
 * You can have multiple installations in one database if you give each
 * a unique prefix. Only numbers, letters, and underscores please!
 */
//$table_prefix  = 'wp_';

/**
 * For developers: WordPress debugging mode.
 *
 * Change this to true to enable the display of notices during development.
 * It is strongly recommended that plugin and theme developers use WP_DEBUG
 * in their development environments.
 *
 * For information on other constants that can be used for debugging,
 * visit the Codex.
 *
 * @link https://codex.wordpress.org/Debugging_in_WordPress
 */
//define('WP_DEBUG', false);

/* That's all, stop editing! Happy blogging. */

/** Absolute path to the WordPress directory. */
if ( !defined('ABSPATH') )
	define('ABSPATH', dirname(__FILE__) . '/');

/** Sets up WordPress vars and included files. */
require_once(ABSPATH . 'wp-settings.php');
