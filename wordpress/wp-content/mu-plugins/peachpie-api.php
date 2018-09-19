<?php
/**
 * @package Peachpied.WordPress
 * @version 1.0.0
 */
/*
Plugin Name: Peachpie API
Plugin URI: https://wordpress.peachpied.com/
Description: Plugin that provides bridge between WordPress API and .NET.
Author: Peachpie
Version: 1.0
*/

namespace PeachPied\WordPress\Sdk;

/** Implementation of IWpApp providing functionality to .NET code */
final class WpAppImpl extends WpApp
{
	/** Calls `add_filter`. */
	function AddFilter(string $tag, \System\Delegate $delegate, int $priority, int $accepted_args) : void { add_filter($tag, $delegate, $priority, $accepted_args); }

	/** Calls `add_shortcode`. */
	function AddShortcode(string $tag, \System\Delegate $delegate) : void { add_shortcode($tag, $delegate); }
}

/** @var \PeachPied\WordPress\Sdk\WpLoader $peachpie_wp_loader  */
$peachpie_wp_loader->AppStarted(new WpAppImpl);
