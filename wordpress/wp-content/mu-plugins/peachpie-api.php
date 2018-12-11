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
Version: 1.0.0
*/

namespace PeachPied\WordPress\Sdk;

/** @var \PeachPied\WordPress\Sdk\WpLoader $peachpie_wp_loader  */
$peachpie_wp_loader->AppStarted(new class extends WpApp
{
	/** Calls `add_filter`. */
	function AddFilter(string $tag, \System\Delegate $delegate, int $priority, int $accepted_args) : void
	{
		add_filter($tag, $delegate, $priority, $accepted_args);
	}

	/** Calls `add_shortcode`. */
	function AddShortcode(string $tag, \System\Delegate $delegate) : void
	{
		add_shortcode($tag, $delegate);
	}
});
