<?php
/**
 * @package PeachPied.WordPress.DotNetBridge
 */
/*
Plugin Name: PeachPied.WordPress.DotNetBridge
Plugin URI: https://github.com/iolevel/wpdotnet-sdk
Description: Plugin that provides bridge between WordPress API and .NET.
Tags: peachpie, wpdotnet
Author: iolevel s.r.o.
*/

namespace PeachPied\WordPress\Standard;

if (!defined('PEACHPIE_VERSION'))
{
	die("This plugin only works on .NET.");
}

// call "AppStarted" event to load additional plugins

/** @var \PeachPied\WordPress\Sdk\WpLoader $peachpie_wp_loader  */
$peachpie_wp_loader->AppStarted(new class extends WpApp
{
	// just an obbject-oriented shortcuts to call wordpress methods,
	// could be done through `Context` API in C#  as well:

	/** Calls `add_filter`. */
	function AddFilter(string $tag, \System\Delegate $delegate, int $priority, int $accepted_args) : void
	{
		\add_filter($tag, $delegate, $priority, $accepted_args);
	}

	/** Calls `add_shortcode`. */
	function AddShortcode(string $tag, \System\Delegate $delegate) : void
	{
		\add_shortcode($tag, $delegate);
	}
});
